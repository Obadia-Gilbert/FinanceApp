import { useState, useMemo } from 'react';
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
import { getTransactions } from '../../../src/api/transactions';
import { formatCurrencyCode } from '../../../src/utils/currency';
import type { TransactionDto } from '../../../src/types/api';

type DateGroup = { label: string; data: TransactionDto[] };
type FilterType = 'all' | 'Income' | 'Expense' | 'Transfer';

function groupByDate(items: TransactionDto[]): DateGroup[] {
  const today = new Date().toDateString();
  const yesterday = new Date(Date.now() - 864e5).toDateString();
  const map = new Map<string, TransactionDto[]>();
  items.forEach((item) => {
    const d = item.date ? new Date(item.date).toDateString() : '';
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
    let label = key === today ? 'TODAY' : key === yesterday ? 'YESTERDAY' : new Date(key).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }).toUpperCase();
    groups.push({ label, data });
  });
  return groups;
}

function iconForTransaction(item: TransactionDto): string {
  if (item.type === 'Income') return '💰';
  if (item.type === 'Transfer') return '↔️';
  if ((item.categoryName || '').toLowerCase().includes('food') || (item.categoryName || '').toLowerCase().includes('dining')) return '🍽️';
  if ((item.categoryName || '').toLowerCase().includes('transport')) return '🚗';
  if ((item.categoryName || '').toLowerCase().includes('shopping')) return '🛒';
  if ((item.categoryName || '').toLowerCase().includes('housing')) return '🏠';
  return '📝';
}

function iconBgForTransaction(item: TransactionDto): string {
  if (item.type === 'Income') return '#10B981';
  if (item.type === 'Transfer') return '#6B7280';
  if ((item.categoryName || '').toLowerCase().includes('food')) return '#F59E0B';
  if ((item.categoryName || '').toLowerCase().includes('transport')) return '#3B82F6';
  if ((item.categoryName || '').toLowerCase().includes('shopping')) return '#8B5CF6';
  if ((item.categoryName || '').toLowerCase().includes('housing')) return '#EF4444';
  return '#6B7280';
}

export default function TransactionsListScreen() {
  const { colors } = useTheme();
  const router = useRouter();
  const [search, setSearch] = useState('');
  const [filter, setFilter] = useState<FilterType>('all');

  const { data, refetch, isRefetching } = useQuery({
    queryKey: ['transactions'],
    queryFn: () => getTransactions(1, 50),
  });

  const list = data?.items ?? [];
  const filtered = useMemo(() => {
    let f = list;
    if (filter !== 'all') f = f.filter((t) => t.type === filter);
    if (search.trim()) {
      const q = search.trim().toLowerCase();
      f = f.filter(
        (t) =>
          (t.note ?? '').toLowerCase().includes(q) ||
          (t.categoryName ?? '').toLowerCase().includes(q) ||
          (t.accountName ?? '').toLowerCase().includes(q)
      );
    }
    return f;
  }, [list, filter, search]);

  const groups = useMemo(() => groupByDate(filtered), [filtered]);

  return (
    <View style={[styles.container, { backgroundColor: colors.bg.alt }]}>
      <View style={[styles.searchWrap, { backgroundColor: colors.bg.default, borderColor: colors.border }]}>
        <Text style={styles.searchIcon}>🔍</Text>
        <TextInput
          style={[styles.searchInput, { color: colors.text.primary }]}
          placeholder="Search transactions..."
          placeholderTextColor={colors.text.subtle}
          value={search}
          onChangeText={setSearch}
        />
      </View>

      <View style={styles.pillsRow}>
        {(['all', 'Income', 'Expense', 'Transfer'] as const).map((f) => (
          <TouchableOpacity
            key={f}
            style={[styles.pill, { backgroundColor: filter === f ? colors.brand : colors.bg.default, borderColor: colors.border }]}
            onPress={() => setFilter(f)}
          >
            <Text style={[styles.pillText, { color: filter === f ? '#fff' : colors.text.primary }]}>
              {f === 'all' ? 'All' : f}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      <FlatList
        data={groups}
        keyExtractor={(g) => g.label}
        contentContainerStyle={styles.listContent}
        refreshControl={<RefreshControl refreshing={isRefetching} onRefresh={() => refetch()} tintColor={colors.brand} />}
        ListEmptyComponent={
          <Card style={styles.empty}>
            <Text style={[styles.emptyTitle, { color: colors.text.primary }]}>No transactions yet</Text>
            <Text style={[styles.emptyBody, { color: colors.text.muted }]}>Add a transaction or transfer</Text>
          </Card>
        }
        renderItem={({ item: group }) => (
          <View style={styles.group}>
            <Text style={[styles.groupLabel, { color: colors.text.muted }]}>{group.label}</Text>
            {group.data.map((item) => (
              <TransactionRow key={item.id} item={item} colors={colors} />
            ))}
          </View>
        )}
      />

      <View style={styles.fabRow}>
        <TouchableOpacity style={[styles.fab, { backgroundColor: colors.brand }]} onPress={() => router.push('/(tabs)/transactions/transfer')} activeOpacity={0.9}>
          <Text style={styles.fabText}>↔</Text>
        </TouchableOpacity>
        <TouchableOpacity style={[styles.fab, { backgroundColor: colors.brand }]} onPress={() => router.push('/(tabs)/transactions/create')} activeOpacity={0.9}>
          <Text style={styles.fabText}>+</Text>
        </TouchableOpacity>
      </View>
    </View>
  );
}

function formatTransactionDate(dateStr: string | null | undefined): string {
  if (!dateStr) return '—';
  const d = new Date(dateStr);
  if (Number.isNaN(d.getTime())) return '—';
  return d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
}

function TransactionRow({ item, colors }: { item: TransactionDto; colors: ReturnType<typeof useTheme>['colors'] }) {
  const isIncome = item.type === 'Income';
  const isTransfer = item.type === 'Transfer';
  const subText = [item.categoryName, item.accountName].filter(Boolean).join(' • ') || item.type;
  const timeStr = formatTransactionDate(item.date);

  let amountColor = colors.text.primary;
  if (isIncome) amountColor = colors.success;
  else if (!isTransfer) amountColor = colors.danger;

  const amountPrefix = isIncome ? '+' : isTransfer ? '' : '−';
  const amountStr = `${amountPrefix}${Number(item.amount).toLocaleString()} ${formatCurrencyCode(item.currency)}`;

  return (
    <TouchableOpacity style={[styles.row, { borderBottomColor: colors.border }]} activeOpacity={0.7}>
      <View style={[styles.rowIcon, { backgroundColor: iconBgForTransaction(item) }]}>
        <Text style={styles.rowIconText}>{iconForTransaction(item)}</Text>
      </View>
      <View style={styles.rowCenter}>
        <Text style={[styles.rowName, { color: colors.text.primary }]} numberOfLines={1}>
          {item.note || item.categoryName || item.accountName || item.type}
        </Text>
        <Text style={[styles.rowSub, { color: colors.text.muted }]} numberOfLines={1}>{subText}</Text>
      </View>
      <View style={styles.rowRight}>
        <Text style={[styles.rowAmount, { color: amountColor }]}>{amountStr}</Text>
        <Text style={[styles.rowTime, { color: colors.text.muted }]}>{timeStr}</Text>
      </View>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  searchWrap: { flexDirection: 'row', alignItems: 'center', marginHorizontal: 16, marginTop: 12, marginBottom: 12, paddingHorizontal: 14, paddingVertical: 10, borderRadius: 12, borderWidth: 1 },
  searchIcon: { fontSize: 16, marginRight: 8 },
  searchInput: { flex: 1, fontSize: 15, paddingVertical: 0 },
  pillsRow: { flexDirection: 'row', paddingHorizontal: 16, marginBottom: 16, gap: 10 },
  pill: { paddingHorizontal: 16, paddingVertical: 10, borderRadius: 20, borderWidth: 1 },
  pillText: { fontSize: 14, fontWeight: '600' },
  listContent: { paddingHorizontal: 16, paddingBottom: 88 },
  group: { marginBottom: 20 },
  groupLabel: { fontSize: 12, fontWeight: '700', marginBottom: 10, letterSpacing: 0.5 },
  row: { flexDirection: 'row', alignItems: 'center', paddingVertical: 12, borderBottomWidth: 1 },
  rowIcon: { width: 44, height: 44, borderRadius: 22, justifyContent: 'center', alignItems: 'center', marginRight: 12 },
  rowIconText: { fontSize: 20 },
  rowCenter: { flex: 1, minWidth: 0 },
  rowName: { fontSize: 16, fontWeight: '600' },
  rowSub: { fontSize: 13, marginTop: 2 },
  rowRight: { alignItems: 'flex-end' },
  rowAmount: { fontSize: 15, fontWeight: '600' },
  rowTime: { fontSize: 12, marginTop: 2 },
  empty: { marginTop: 24 },
  emptyTitle: { fontSize: 16, fontWeight: '600', marginBottom: 8 },
  emptyBody: { fontSize: 14 },
  fabRow: { position: 'absolute', right: 20, bottom: 24, flexDirection: 'row', gap: 12 },
  fab: { width: 56, height: 56, borderRadius: 28, justifyContent: 'center', alignItems: 'center', shadowColor: '#000', shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.25, shadowRadius: 4, elevation: 5 },
  fabText: { color: '#fff', fontSize: 24 },
});
