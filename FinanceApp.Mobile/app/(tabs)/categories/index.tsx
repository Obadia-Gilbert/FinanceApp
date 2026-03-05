import { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  RefreshControl,
  TextInput,
} from 'react-native';
import { useRouter } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { useTheme } from '../../../src/context/ThemeContext';
import { Card } from '../../../src/components/Card';
import { getCategories } from '../../../src/api/categories';
import { getCategoryBudgets } from '../../../src/api/budget';
import { formatCurrencyCode } from '../../../src/utils/currency';

const now = new Date();
const thisMonth = now.getMonth() + 1;
const thisYear = now.getFullYear();

/** Map API icon names (e.g. "shopping-cart") or category names to emoji for display. */
function categoryIconEmoji(icon: string | null | undefined, name: string): string {
  const n = (name || '').toLowerCase();
  const i = (icon || '').toLowerCase();
  // Name-based mapping (most reliable)
  if (n.includes('food') || n.includes('grocer') || i.includes('cart') || i.includes('shop')) return '🛒';
  if (n.includes('employment') || n.includes('salary') || n.includes('work') || i.includes('briefcase')) return '💼';
  if (n.includes('utilit') || n.includes('bill') || i.includes('light') || i.includes('plug')) return '💡';
  if (n.includes('entertainment') || n.includes('fun') || i.includes('film') || i.includes('game')) return '🎬';
  if (n.includes('invest') || n.includes('asset') || i.includes('graph') || i.includes('chart')) return '📈';
  if (n.includes('freelance') || n.includes('side') || n.includes('gig')) return '💻';
  if (n.includes('transport') || n.includes('car') || i.includes('car')) return '🚗';
  if (n.includes('health') || i.includes('heart')) return '❤️';
  if (n.includes('housing') || n.includes('rent') || i.includes('house')) return '🏠';
  if (n.includes('education') || i.includes('book')) return '📚';
  if (n.includes('dining') || n.includes('restaurant')) return '🍽️';
  if (i.includes('doc') || i.includes('file')) return '📄';
  // Single character or emoji from API – use as-is
  if (icon && icon.length <= 2 && !/^[a-z-]+$/i.test(icon)) return icon;
  // Default
  return '📁';
}

export default function ManageCategoriesScreen() {
  const { colors } = useTheme();
  const router = useRouter();
  const [search, setSearch] = useState('');

  const { data: categories = [], refetch, isRefetching } = useQuery({
    queryKey: ['categories'],
    queryFn: getCategories,
  });

  const { data: categoryBudgets = [] } = useQuery({
    queryKey: ['categoryBudgets', thisMonth, thisYear],
    queryFn: async () => {
      try {
        return await getCategoryBudgets(thisMonth, thisYear);
      } catch {
        return [];
      }
    },
  });

  const getBudgetForCategory = (categoryId: string) =>
    categoryBudgets.find((cb) => cb.categoryId === categoryId);

  const filtered = search.trim()
    ? categories.filter((c) => c.name.toLowerCase().includes(search.trim().toLowerCase()))
    : categories;

  return (
    <View style={[styles.container, { backgroundColor: colors.bg.alt }]}>
      <TouchableOpacity
        style={[styles.newCategoryBtn, { backgroundColor: colors.brand }]}
        onPress={() => router.push('/(tabs)/categories/create')}
      >
        <Text style={styles.newCategoryIcon}>+</Text>
        <Text style={styles.newCategoryText}>New Category</Text>
      </TouchableOpacity>

      <View style={styles.sectionHeader}>
        <Text style={[styles.sectionTitle, { color: colors.text.primary }]}>All Categories</Text>
        <Text style={[styles.sectionCount, { color: colors.text.muted }]}>{categories.length} Total</Text>
      </View>

      <View style={[styles.searchWrap, { backgroundColor: colors.bg.default, borderColor: colors.border }]}>
        <Text style={styles.searchIcon}>🔍</Text>
        <TextInput
          style={[styles.searchInput, { color: colors.text.primary }]}
          placeholder="Search categories..."
          placeholderTextColor={colors.text.subtle}
          value={search}
          onChangeText={setSearch}
        />
      </View>

      <FlatList
        data={filtered}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.list}
        refreshControl={
          <RefreshControl refreshing={isRefetching} onRefresh={() => refetch()} tintColor={colors.brand} />
        }
        ListEmptyComponent={
          <Card style={styles.empty}>
            <Text style={[styles.emptyText, { color: colors.text.muted }]}>
              No categories yet. Tap "New Category" to add one.
            </Text>
          </Card>
        }
        renderItem={({ item }) => {
          const cb = getBudgetForCategory(item.id);
          const budgetStr = cb
            ? `${formatCurrencyCode(cb.currency as string | number)} ${cb.amount.toLocaleString()}`
            : '—';
          return (
            <Card style={[styles.row, { borderColor: colors.border }]}>
              <View style={[styles.iconWrap, { backgroundColor: item.badgeColor || colors.brand }]}>
                <Text style={styles.iconEmoji}>{categoryIconEmoji(item.icon, item.name)}</Text>
              </View>
              <View style={styles.rowBody}>
                <Text style={[styles.name, { color: colors.text.primary }]}>{item.name}</Text>
                <Text style={[styles.budgetLabel, { color: colors.text.muted }]}>
                  Monthly budget: {budgetStr}
                </Text>
              </View>
              <TouchableOpacity
                style={[styles.editBtn, { backgroundColor: colors.bg.alt, borderColor: colors.border }]}
                onPress={() => router.push(`/(tabs)/categories/${item.id}`)}
              >
                <Text style={[styles.editBtnText, { color: colors.text.primary }]}>Edit</Text>
              </TouchableOpacity>
            </Card>
          );
        }}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  newCategoryBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 10,
    marginHorizontal: 16,
    marginTop: 16,
    marginBottom: 20,
    paddingVertical: 14,
    borderRadius: 14,
  },
  newCategoryIcon: { color: '#fff', fontSize: 22, fontWeight: '600' },
  newCategoryText: { color: '#fff', fontSize: 16, fontWeight: '600' },
  sectionHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingHorizontal: 16, marginBottom: 12 },
  sectionTitle: { fontSize: 18, fontWeight: '700' },
  sectionCount: { fontSize: 14 },
  searchWrap: {
    flexDirection: 'row',
    alignItems: 'center',
    marginHorizontal: 16,
    marginBottom: 16,
    paddingHorizontal: 14,
    paddingVertical: 10,
    borderRadius: 10,
    borderWidth: 1,
  },
  searchIcon: { fontSize: 16, marginRight: 8 },
  searchInput: { flex: 1, fontSize: 15, paddingVertical: 0 },
  list: { paddingHorizontal: 16, paddingBottom: 40 },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 14,
    marginBottom: 10,
    borderWidth: 1,
  },
  iconWrap: {
    width: 44,
    height: 44,
    borderRadius: 12,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  iconEmoji: { fontSize: 22 },
  rowBody: { flex: 1, minWidth: 0 },
  name: { fontSize: 16, fontWeight: '600' },
  budgetLabel: { fontSize: 13, marginTop: 2 },
  editBtn: { paddingHorizontal: 16, paddingVertical: 8, borderRadius: 8, borderWidth: 1 },
  editBtnText: { fontSize: 14, fontWeight: '600' },
  empty: { padding: 24 },
  emptyText: { fontSize: 14, textAlign: 'center' },
});
