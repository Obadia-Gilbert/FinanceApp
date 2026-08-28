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
import { Icon } from '../../../src/components/Icon';
import { useRouter } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { useTheme } from '../../../src/context/ThemeContext';
import { categoryIcon } from '../../../src/utils/categoryIcon';
import { Card } from '../../../src/components/Card';
import { getCategories } from '../../../src/api/categories';
import { getCategoryBudgets } from '../../../src/api/budget';
import { formatCurrencyCode, formatAmount } from '../../../src/utils/currency';

const now = new Date();
const thisMonth = now.getMonth() + 1;
const thisYear = now.getFullYear();

/** Map API icon names (e.g. "shopping-cart") or category names to emoji for display. */

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
        <Text style={[styles.newCategoryIcon, { color: colors.brandContrast }]}>+</Text>
        <Text style={[styles.newCategoryText, { color: colors.brandContrast }]}>New Category</Text>
      </TouchableOpacity>

      <View style={styles.sectionHeader}>
        <Text style={[styles.sectionTitle, { color: colors.text.primary }]}>All Categories</Text>
        <Text style={[styles.sectionCount, { color: colors.text.muted }]}>{categories.length} Total</Text>
      </View>

      <View style={[styles.searchWrap, { backgroundColor: colors.bg.default, borderColor: colors.border }]}>
        <Icon name="search" size={18} color={colors.text.muted} style={styles.searchIcon} />
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
              No categories yet. Tap &quot;New Category&quot; to add one.
            </Text>
          </Card>
        }
        renderItem={({ item }) => {
          const cb = getBudgetForCategory(item.id);
          const budgetStr = cb
            ? `${formatCurrencyCode(cb.currency)} ${formatAmount(cb.amount, cb.currency)}`
            : '—';
          return (
            <Card style={[styles.row, { borderColor: colors.border }]}>
              <View style={[styles.iconWrap, { backgroundColor: item.badgeColor || colors.brand }]}>
                <Text style={styles.iconEmoji}>{categoryIcon(item.name, item.icon)}</Text>
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
  newCategoryIcon: { fontSize: 22, fontWeight: '600' },
  newCategoryText: { fontSize: 16, fontWeight: '600' },
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
  searchIcon: { marginRight: 8 },
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
