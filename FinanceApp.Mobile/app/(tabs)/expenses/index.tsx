import { useState, useMemo } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  RefreshControl,
  ActivityIndicator,
  TextInput,
  ScrollView,
} from 'react-native';
import { useRouter } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { useTheme } from '../../../src/context/ThemeContext';
import { Card } from '../../../src/components/Card';
import { getExpenses } from '../../../src/api/expenses';
import { getCategories } from '../../../src/api/categories';
import { formatCurrencyCode } from '../../../src/utils/currency';
import type { ExpenseDto } from '../../../src/types/api';

type DateGroup = { label: string; data: ExpenseDto[] };

const CATEGORY_ICONS: Record<string, string> = {
  food: '🍽️',
  transport: '🚗',
  entertainment: '🎬',
  shopping: '🛒',
  health: '❤️',
  bills: '📄',
  rent: '🏠',
  utility: '⚡',
  education: '📚',
  travel: '✈️',
  personal: '👤',
  insurance: '🛡',
  default: '💸',
};

function categoryIcon(name: string | null): string {
  if (!name) return CATEGORY_ICONS.default;
  const key = Object.keys(CATEGORY_ICONS).find((k) => name.toLowerCase().includes(k));
  return key ? CATEGORY_ICONS[key] : CATEGORY_ICONS.default;
}

function groupByDate(items: ExpenseDto[]): DateGroup[] {
  const today = new Date().toDateString();
  const yesterday = new Date(Date.now() - 864e5).toDateString();
  const map = new Map<string, ExpenseDto[]>();
  items.forEach((item) => {
    const d = item.expenseDate ? new Date(item.expenseDate).toDateString() : '';
    if (!map.has(d)) map.set(d, []);
    map.get(d)!.push(item);
  });
  const groups: DateGroup[] = [];
  const order = [today, yesterday];
  const rest = [...map.keys()].filter((k) => !order.includes(k));
  rest.sort((a, b) => new Date(b).getTime() - new Date(a).getTime());
  [...order, ...rest].forEach((key) => {
    const data = map.get(key);
    if (!data?.length) return;
    let label = key;
    if (key === today) label = 'TODAY';
    else if (key === yesterday) label = 'YESTERDAY';
    else label = new Date(key).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }).toUpperCase();
    groups.push({ label, data });
  });
  return groups;
}

export default function ExpensesListScreen() {
  const { colors, isDark } = useTheme();
  const router = useRouter();
  const [search, setSearch] = useState('');
  const [categoryId, setCategoryId] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const pageSize = 50;

  const { data: catData } = useQuery({ queryKey: ['categories'], queryFn: getCategories });
  const categories = catData ?? [];

  const { data, isLoading, isError, error, refetch, isRefetching } = useQuery({
    queryKey: ['expenses', page, categoryId ?? 'all'],
    queryFn: () => getExpenses(page, pageSize, categoryId ?? undefined),
  });

  const list = data?.items ?? [];
  const filtered = useMemo(() => {
    if (!search.trim()) return list;
    const q = search.trim().toLowerCase();
    return list.filter(
      (e) =>
        (e.description ?? '').toLowerCase().includes(q) ||
        (e.categoryName ?? '').toLowerCase().includes(q)
    );
  }, [list, search]);

  const groups = useMemo(() => groupByDate(filtered), [filtered]);

  if (isError) {
    return (
      <View style={[styles.centered, { backgroundColor: colors.bg.alt }]}>
        <Text style={{ fontSize: 48, marginBottom: 12 }}>💸</Text>
        <Text style={[styles.errorText, { color: colors.danger }]}>
          {(error as Error)?.message ?? 'Failed to load expenses'}
        </Text>
        <Text style={{ fontSize: 14, marginTop: 8, color: colors.text.muted }}>Pull down to retry</Text>
      </View>
    );
  }

  return (
    <View style={[styles.container, { backgroundColor: colors.bg.alt }]}>
      {/* Search */}
      <View style={styles.searchRow}>
        <View style={[styles.searchWrap, { backgroundColor: colors.bg.default, borderColor: colors.border }]}>
          <Text style={styles.searchIcon}>🔍</Text>
          <TextInput
            style={[styles.searchInput, { color: colors.text.primary }]}
            placeholder="Search expenses..."
            placeholderTextColor={colors.text.subtle}
            value={search}
            onChangeText={setSearch}
          />
          {search.length > 0 && (
            <TouchableOpacity onPress={() => setSearch('')}>
              <Text style={[{ fontSize: 16, color: colors.text.subtle }]}>✕</Text>
            </TouchableOpacity>
          )}
        </View>
      </View>

      {/* Category pills */}
      <View style={styles.pillsSection}>
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={[styles.pillsContent, { paddingHorizontal: 16 }]}
          style={styles.pillsScroll}
        >
          <TouchableOpacity
            style={[
              styles.pill,
              { backgroundColor: categoryId === null ? colors.brand : colors.bg.default, borderColor: categoryId === null ? colors.brand : colors.border },
            ]}
            onPress={() => setCategoryId(null)}
          >
            <Text style={[styles.pillText, { color: categoryId === null ? '#fff' : colors.text.primary }]}>All</Text>
          </TouchableOpacity>
          {categories.filter(c => c.type === 'Expense' || c.type === 'Both').map((c) => (
            <TouchableOpacity
              key={c.id}
              style={[
                styles.pill,
                { backgroundColor: categoryId === c.id ? colors.brand : colors.bg.default, borderColor: categoryId === c.id ? colors.brand : colors.border },
              ]}
              onPress={() => setCategoryId(categoryId === c.id ? null : c.id)}
            >
              <Text style={[styles.pillText, { color: categoryId === c.id ? '#fff' : colors.text.primary }]}>
                {c.name}
              </Text>
            </TouchableOpacity>
          ))}
        </ScrollView>
      </View>

      <FlatList
        data={groups}
        keyExtractor={(g) => g.label}
        contentContainerStyle={styles.listContent}
        refreshControl={
          <RefreshControl refreshing={isRefetching && !isLoading} onRefresh={() => refetch()} tintColor={colors.brand} />
        }
        ListEmptyComponent={
          isLoading ? (
            <View style={styles.loading}>
              <ActivityIndicator size="large" color={colors.brand} />
            </View>
          ) : (
            <View style={styles.emptyWrap}>
              <Text style={{ fontSize: 48, marginBottom: 12 }}>💸</Text>
              <Text style={[styles.emptyTitle, { color: colors.text.primary }]}>No expenses yet</Text>
              <Text style={[styles.emptyBody, { color: colors.text.muted }]}>Tap + to add your first expense</Text>
            </View>
          )
        }
        renderItem={({ item: group }) => (
          <View style={styles.group}>
            <Text style={[styles.groupLabel, { color: colors.text.muted }]}>{group.label}</Text>
            {group.data.map((item) => (
              <ExpenseRow
                key={item.id}
                item={item}
                colors={colors}
                onPress={() => router.push(`/(tabs)/expenses/${item.id}`)}
              />
            ))}
          </View>
        )}
      />

      <TouchableOpacity
        style={[styles.fab, { backgroundColor: colors.brand }]}
        onPress={() => router.push(`/(tabs)/expenses/create?t=${Date.now()}`)}
        activeOpacity={0.9}
        hitSlop={{ top: 12, bottom: 12, left: 12, right: 12 }}
        accessibilityRole="button"
        accessibilityLabel="Add expense"
      >
        <Text style={styles.fabText}>+</Text>
      </TouchableOpacity>
    </View>
  );
}

function ExpenseRow({
  item,
  colors,
  onPress,
}: {
  item: ExpenseDto;
  colors: ReturnType<typeof useTheme>['colors'];
  onPress: () => void;
}) {
  const date = item.expenseDate ? new Date(item.expenseDate) : null;
  const time = date ? date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' }) : '—';
  const icon = categoryIcon(item.categoryName);

  return (
    <TouchableOpacity onPress={onPress} activeOpacity={0.7} style={[styles.rowTouch, { borderBottomColor: colors.border }]}>
      <View style={[styles.rowIcon, { backgroundColor: `${colors.danger}12` }]}>
        <Text style={styles.rowIconText}>{icon}</Text>
      </View>
      <View style={styles.rowCenter}>
        <Text style={[styles.rowMerchant, { color: colors.text.primary }]} numberOfLines={1}>
          {item.description || 'No description'}
        </Text>
        <Text style={[styles.rowAccount, { color: colors.text.muted }]} numberOfLines={1}>
          {item.categoryName ?? 'Uncategorized'}
        </Text>
      </View>
      <View style={styles.rowRight}>
        <Text style={[styles.rowAmount, { color: colors.danger }]}>
          −{Number(item.amount).toLocaleString()} {formatCurrencyCode(item.currency)}
        </Text>
        <Text style={[styles.rowTime, { color: colors.text.subtle }]}>{time}</Text>
      </View>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  centered: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 24 },
  errorText: { fontSize: 16, textAlign: 'center' },
  searchRow: { flexDirection: 'row', alignItems: 'center', paddingHorizontal: 16, paddingTop: 12, gap: 10 },
  searchWrap: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    borderRadius: 12,
    borderWidth: 1,
    paddingHorizontal: 14,
    paddingVertical: 10,
  },
  searchIcon: { fontSize: 16, marginRight: 8 },
  searchInput: { flex: 1, fontSize: 15, paddingVertical: 0 },
  pillsSection: { minHeight: 52, marginBottom: 8 },
  pillsScroll: { flexGrow: 0, minHeight: 44 },
  pillsContent: { flexDirection: 'row', alignItems: 'center', paddingVertical: 12 },
  pill: {
    flexDirection: 'row',
    alignItems: 'center',
    alignSelf: 'center',
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderRadius: 20,
    marginRight: 8,
    borderWidth: 1,
  },
  pillText: { fontSize: 14, fontWeight: '600' },
  listContent: { padding: 16, paddingBottom: 88 },
  loading: { padding: 40, alignItems: 'center' },
  emptyWrap: { paddingTop: 60, alignItems: 'center', paddingHorizontal: 24 },
  emptyTitle: { fontSize: 18, fontWeight: '600', marginBottom: 8 },
  emptyBody: { fontSize: 14 },
  group: { marginBottom: 20 },
  groupLabel: { fontSize: 12, fontWeight: '700', marginBottom: 10, letterSpacing: 0.5 },
  rowTouch: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    borderBottomWidth: 1,
  },
  rowIcon: {
    width: 44,
    height: 44,
    borderRadius: 14,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  rowIconText: { fontSize: 20 },
  rowCenter: { flex: 1, minWidth: 0 },
  rowMerchant: { fontSize: 16, fontWeight: '600' },
  rowAccount: { fontSize: 13, marginTop: 2 },
  rowRight: { alignItems: 'flex-end' },
  rowAmount: { fontSize: 15, fontWeight: '600' },
  rowTime: { fontSize: 12, marginTop: 2 },
  fab: {
    position: 'absolute',
    right: 20,
    bottom: 24,
    width: 56,
    height: 56,
    borderRadius: 28,
    justifyContent: 'center',
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.25,
    shadowRadius: 4,
    elevation: 5,
    zIndex: 10,
  },
  fabText: { color: '#fff', fontSize: 28, fontWeight: '300', lineHeight: 30 },
});
