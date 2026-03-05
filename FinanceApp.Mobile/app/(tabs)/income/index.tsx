import { useState, useMemo } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  RefreshControl,
  TextInput,
  ScrollView,
} from 'react-native';
import { useRouter } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { useTheme } from '../../../src/context/ThemeContext';
import { Card } from '../../../src/components/Card';
import { getIncomes } from '../../../src/api/income';
import { formatCurrencyCode } from '../../../src/utils/currency';
import type { IncomeDto } from '../../../src/types/api';

const MONTHS = 'Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov Dec'.split(' ');
const INCOME_FILTERS = ['All Income', 'Salary', 'Side Hustles', 'Dividends'];

function iconForCategory(name: string | null): string {
  const n = (name || '').toLowerCase();
  if (n.includes('salary')) return '💰';
  if (n.includes('freelance') || n.includes('side')) return '💻';
  if (n.includes('dividend')) return '📈';
  if (n.includes('rental') || n.includes('rent')) return '🏠';
  return '💰';
}

function bgForCategory(name: string | null): string {
  const n = (name || '').toLowerCase();
  if (n.includes('salary')) return '#10B981';
  if (n.includes('freelance') || n.includes('side')) return '#3B82F6';
  if (n.includes('dividend')) return '#8B5CF6';
  if (n.includes('rental') || n.includes('rent')) return '#F59E0B';
  return '#10B981';
}

export default function IncomeListScreen() {
  const { colors } = useTheme();
  const router = useRouter();
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState(0); // 0 = All, 1 = Salary, etc.

  const now = new Date();
  const thisMonth = now.getMonth();
  const monthLabel = MONTHS[thisMonth];
  const year = now.getFullYear();

  const { data, isLoading, isError, error, refetch, isRefetching } = useQuery({
    queryKey: ['incomes'],
    queryFn: () => getIncomes(1, 50),
  });

  const list = data?.items ?? [];
  const filtered = useMemo(() => {
    let f = list;
    const filterLabel = INCOME_FILTERS[categoryFilter];
    if (filterLabel !== 'All Income') {
      const key = filterLabel === 'Side Hustles' ? 'side' : filterLabel === 'Dividends' ? 'dividend' : filterLabel.toLowerCase();
      f = f.filter((i) => (i.categoryName || '').toLowerCase().includes(key));
    }
    if (search.trim()) {
      const q = search.trim().toLowerCase();
      f = f.filter(
        (i) =>
          (i.description || '').toLowerCase().includes(q) ||
          (i.source || '').toLowerCase().includes(q) ||
          (i.categoryName || '').toLowerCase().includes(q)
      );
    }
    return f;
  }, [list, categoryFilter, search]);

  const totalIncome = useMemo(() => list.reduce((s, i) => s + Number(i.amount), 0), [list]);

  if (isError) {
    return (
      <View style={[styles.centered, { backgroundColor: colors.bg.alt }]}>
        <Text style={[styles.errorText, { color: colors.danger }]}>
          {(error as Error)?.message ?? 'Failed to load income'}
        </Text>
      </View>
    );
  }

  const ListHeader = () => (
    <>
      <View style={[styles.totalCard, { backgroundColor: colors.brandLight ?? '#EFF6FF' }]}>
        <Text style={[styles.totalLabel, { color: colors.text.muted }]}>TOTAL INCOME ({monthLabel.toUpperCase()})</Text>
        <Text style={[styles.totalAmount, { color: colors.text.primary }]}>
          {formatCurrencyCode(list[0]?.currency ?? 'USD')} {totalIncome.toLocaleString()}
        </Text>
        <View style={styles.totalTrend}>
          <Text style={styles.trendIcon}>↗</Text>
          <Text style={[styles.trendText, { color: colors.success }]}>+12.5%</Text>
        </View>
      </View>
      <View style={styles.pillsSection}>
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={styles.pills}
          style={styles.pillsScroll}
        >
          {INCOME_FILTERS.map((label, i) => (
            <TouchableOpacity
              key={label}
              style={[
                styles.pill,
                {
                  backgroundColor: categoryFilter === i ? colors.brand : colors.bg.default,
                  borderWidth: 1,
                  borderColor: categoryFilter === i ? colors.brand : colors.border,
                },
              ]}
              onPress={() => setCategoryFilter(i)}
            >
              <Text
                style={[styles.pillText, { color: categoryFilter === i ? '#fff' : colors.text.primary }]}
                numberOfLines={1}
              >
                {label}
              </Text>
            </TouchableOpacity>
          ))}
        </ScrollView>
      </View>
      <Text style={[styles.sectionTitle, { color: colors.text.primary }]}>RECENT TRANSACTIONS</Text>
    </>
  );

  return (
    <View style={[styles.container, { backgroundColor: colors.bg.alt }]}>
      <FlatList
        data={filtered}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.listContent}
        ListHeaderComponent={<ListHeader />}
        refreshControl={
          <RefreshControl refreshing={isRefetching && !isLoading} onRefresh={() => refetch()} tintColor={colors.brand} />
        }
        ListEmptyComponent={
          !isLoading ? (
            <Card style={styles.empty}>
              <Text style={[styles.emptyTitle, { color: colors.text.primary }]}>No income yet</Text>
              <Text style={[styles.emptyBody, { color: colors.text.muted }]}>Tap + to add income</Text>
            </Card>
          ) : null
        }
        renderItem={({ item }) => (
          <TouchableOpacity onPress={() => router.push(`/(tabs)/income/${item.id}`)} activeOpacity={0.7}>
            <Card style={[styles.row, { borderColor: colors.border }]}>
              <View style={[styles.rowIcon, { backgroundColor: bgForCategory(item.categoryName) }]}>
                <Text style={styles.rowIconText}>{iconForCategory(item.categoryName)}</Text>
              </View>
              <View style={styles.rowBody}>
                <Text style={[styles.rowTitle, { color: colors.text.primary }]} numberOfLines={1}>
                  {item.description || item.source || item.categoryName || 'Income'}
                </Text>
                <Text style={[styles.rowSub, { color: colors.text.muted }]}>
                  {item.incomeDate ? new Date(item.incomeDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }) : ''} • {item.accountName || '—'}
                </Text>
              </View>
              <Text style={[styles.rowAmount, { color: colors.success }]}>
                +{Number(item.amount).toLocaleString()} {formatCurrencyCode(item.currency)}
              </Text>
            </Card>
          </TouchableOpacity>
        )}
      />

      <TouchableOpacity
        style={[styles.fab, { backgroundColor: colors.brand }]}
        onPress={() => router.push('/(tabs)/income/create')}
        activeOpacity={0.9}
      >
        <Text style={styles.fabText}>+</Text>
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  centered: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 24 },
  errorText: { fontSize: 16 },
  totalCard: { borderRadius: 16, padding: 20, marginHorizontal: 16, marginTop: 16, marginBottom: 16 },
  totalLabel: { fontSize: 12, letterSpacing: 0.5, marginBottom: 6 },
  totalAmount: { fontSize: 28, fontWeight: '700' },
  totalTrend: { flexDirection: 'row', alignItems: 'center', marginTop: 8 },
  trendIcon: { fontSize: 14, marginRight: 4 },
  trendText: { fontSize: 14, fontWeight: '600' },
  pillsSection: { marginBottom: 16, minHeight: 48 },
  pillsScroll: { flexGrow: 0 },
  pills: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 10,
    paddingHorizontal: 16,
    gap: 10,
  },
  pill: {
    paddingHorizontal: 18,
    paddingVertical: 12,
    borderRadius: 22,
  },
  pillText: { fontSize: 15, fontWeight: '600' },
  sectionTitle: { fontSize: 14, fontWeight: '700', letterSpacing: 0.5, marginBottom: 12, marginHorizontal: 16 },
  listContent: { paddingHorizontal: 16, paddingBottom: 88 },
  row: { flexDirection: 'row', alignItems: 'center', padding: 14, marginBottom: 10, borderWidth: 1 },
  rowIcon: { width: 44, height: 44, borderRadius: 22, justifyContent: 'center', alignItems: 'center', marginRight: 12 },
  rowIconText: { fontSize: 22 },
  rowBody: { flex: 1, minWidth: 0 },
  rowTitle: { fontSize: 16, fontWeight: '600' },
  rowSub: { fontSize: 13, marginTop: 2 },
  rowAmount: { fontSize: 16, fontWeight: '600' },
  empty: { marginTop: 24 },
  emptyTitle: { fontSize: 16, fontWeight: '600', marginBottom: 8 },
  emptyBody: { fontSize: 14 },
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
  },
  fabText: { color: '#fff', fontSize: 28, fontWeight: '300' },
});
