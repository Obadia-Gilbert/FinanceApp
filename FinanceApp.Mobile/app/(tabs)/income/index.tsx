import { useState, useMemo } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  RefreshControl,
  ScrollView,
  ActivityIndicator,
} from 'react-native';
import { useRouter } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { useTheme } from '../../../src/context/ThemeContext';
import { Card } from '../../../src/components/Card';
import { getIncomes } from '../../../src/api/income';
import { getCategories } from '../../../src/api/categories';
import { formatCurrencyCode } from '../../../src/utils/currency';
import type { IncomeDto } from '../../../src/types/api';

const MONTHS = 'Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov Dec'.split(' ');

const CATEGORY_ICONS: Record<string, string> = {
  salary: '💼',
  freelance: '💻',
  investment: '📈',
  gift: '🎁',
  rental: '🏠',
  business: '🏢',
  dividend: '💵',
  default: '💰',
};

function iconForCategory(name: string | null): string {
  if (!name) return CATEGORY_ICONS.default;
  const key = Object.keys(CATEGORY_ICONS).find((k) => name.toLowerCase().includes(k));
  return key ? CATEGORY_ICONS[key] : CATEGORY_ICONS.default;
}

export default function IncomeListScreen() {
  const { colors } = useTheme();
  const router = useRouter();
  const [search, setSearch] = useState('');
  const [categoryId, setCategoryId] = useState<string | null>(null);

  const now = new Date();
  const thisMonth = now.getMonth();
  const monthLabel = MONTHS[thisMonth];

  const { data: categories = [] } = useQuery({ queryKey: ['categories'], queryFn: getCategories });
  const incomeCategories = categories.filter((c) => c.type === 'Income' || c.type === 'Both');
  const filterLabels = ['All Income', ...incomeCategories.map((c) => c.name)];
  const selectedFilterIndex = categoryId === null ? 0 : incomeCategories.findIndex((c) => c.id === categoryId) + 1;

  const { data, isLoading, isError, error, refetch, isRefetching } = useQuery({
    queryKey: ['incomes'],
    queryFn: () => getIncomes(1, 50),
  });

  const list = data?.items ?? [];
  const filtered = useMemo(() => {
    let f = list;
    if (categoryId) {
      f = f.filter((i) => i.categoryId === categoryId);
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
  }, [list, categoryId, search]);

  const totalIncome = useMemo(() => list.reduce((s, i) => s + Number(i.amount), 0), [list]);

  if (isError) {
    return (
      <View style={[styles.centered, { backgroundColor: colors.bg.alt }]}>
        <Text style={{ fontSize: 48, marginBottom: 12 }}>📥</Text>
        <Text style={[styles.errorText, { color: colors.danger }]}>
          {(error as Error)?.message ?? 'Failed to load income'}
        </Text>
        <Text style={[{ fontSize: 14, marginTop: 8, color: colors.text.muted }]}>Pull down to retry</Text>
      </View>
    );
  }

  const ListHeader = () => (
    <>
      <View style={[styles.totalCard, { backgroundColor: colors.brandLight ?? colors.bg.alt }]}>
        <Text style={[styles.totalLabel, { color: colors.text.muted }]}>TOTAL INCOME ({monthLabel.toUpperCase()})</Text>
        <Text style={[styles.totalAmount, { color: colors.text.primary }]}>
          {formatCurrencyCode(list[0]?.currency ?? 'USD')} {totalIncome.toLocaleString()}
        </Text>
        <View style={styles.totalTrend}>
          <Text style={[styles.trendIcon, { color: colors.success }]}>↗</Text>
          <Text style={[styles.trendText, { color: colors.success }]}>
            {filtered.length} transaction{filtered.length !== 1 ? 's' : ''}
          </Text>
        </View>
      </View>
      <View style={styles.pillsSection}>
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={styles.pills}
          style={styles.pillsScroll}
        >
          {filterLabels.map((label, i) => (
            <TouchableOpacity
              key={i === 0 ? 'all' : incomeCategories[i - 1]?.id ?? i}
              style={[
                styles.pill,
                {
                  backgroundColor: selectedFilterIndex === i ? colors.brand : colors.bg.default,
                  borderWidth: 1,
                  borderColor: selectedFilterIndex === i ? colors.brand : colors.border,
                },
              ]}
              onPress={() => setCategoryId(i === 0 ? null : incomeCategories[i - 1]?.id ?? null)}
            >
              <Text
                style={[styles.pillText, { color: selectedFilterIndex === i ? '#fff' : colors.text.primary }]}
                numberOfLines={1}
              >
                {label}
              </Text>
            </TouchableOpacity>
          ))}
        </ScrollView>
      </View>
      <Text style={[styles.sectionTitle, { color: colors.text.muted }]}>RECENT TRANSACTIONS</Text>
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
          isLoading ? (
            <View style={styles.loadingWrap}>
              <ActivityIndicator size="large" color={colors.brand} />
            </View>
          ) : (
            <Card style={styles.empty}>
              <Text style={{ fontSize: 48, textAlign: 'center', marginBottom: 12 }}>📥</Text>
              <Text style={[styles.emptyTitle, { color: colors.text.primary }]}>No income yet</Text>
              <Text style={[styles.emptyBody, { color: colors.text.muted }]}>Tap + to add your first income</Text>
            </Card>
          )
        }
        renderItem={({ item }) => (
          <TouchableOpacity onPress={() => router.push(`/(tabs)/income/${item.id}`)} activeOpacity={0.7}>
            <Card style={[styles.row, { borderColor: colors.border }]}>
              <View style={[styles.rowIcon, { backgroundColor: `${colors.success}15` }]}>
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
        hitSlop={{ top: 12, bottom: 12, left: 12, right: 12 }}
      >
        <Text style={styles.fabText}>+</Text>
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  centered: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 24 },
  errorText: { fontSize: 16, textAlign: 'center' },
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
  empty: { marginTop: 24, padding: 24, alignItems: 'center' },
  emptyTitle: { fontSize: 16, fontWeight: '600', marginBottom: 8 },
  emptyBody: { fontSize: 14 },
  loadingWrap: { padding: 40, alignItems: 'center' },
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
  fabText: { color: '#fff', fontSize: 28, fontWeight: '300' },
});
