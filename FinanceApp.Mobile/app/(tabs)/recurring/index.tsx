import { View, Text, StyleSheet, FlatList, TouchableOpacity, RefreshControl } from 'react-native';
import { useRouter } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useTheme } from '../../../src/context/ThemeContext';
import { Card } from '../../../src/components/Card';
import { getRecurringTemplates } from '../../../src/api/recurring';
import { formatCurrencyCode } from '../../../src/utils/currency';
import type { RecurringTemplateDto } from '../../../src/types/api';

const FREQUENCY_LABELS: Record<number, string> = { 0: 'Weekly', 1: 'Monthly', 2: 'Yearly' };

export default function RecurringListScreen() {
  const { colors } = useTheme();
  const router = useRouter();
  const insets = useSafeAreaInsets();

  const { data, refetch, isRefetching } = useQuery({
    queryKey: ['recurring'],
    queryFn: () => getRecurringTemplates(1, 50),
  });

  const list = data?.items ?? [];

  return (
    <View style={[styles.container, { backgroundColor: colors.bg.alt }]}>
      <FlatList
        data={list}
        keyExtractor={(item) => item.id}
        contentContainerStyle={[styles.list, { paddingBottom: insets.bottom + 88 }]}
        refreshControl={
          <RefreshControl refreshing={isRefetching} onRefresh={() => refetch()} tintColor={colors.brand} />
        }
        ListEmptyComponent={
          <Card style={styles.empty}>
            <Text style={[styles.emptyTitle, { color: colors.text.primary }]}>No recurring items yet</Text>
            <Text style={[styles.emptyBody, { color: colors.text.muted }]}>
              Tap + to add a recurring income or expense.
            </Text>
          </Card>
        }
        renderItem={({ item }) => (
          <RecurringRow item={item} colors={colors} />
        )}
      />
      <TouchableOpacity
        style={[styles.fab, { backgroundColor: colors.brand }]}
        onPress={() => router.push('/(tabs)/recurring/create')}
        activeOpacity={0.9}
      >
        <Text style={styles.fabText}>+</Text>
      </TouchableOpacity>
    </View>
  );
}

function RecurringRow({ item, colors }: { item: RecurringTemplateDto; colors: ReturnType<typeof useTheme>['colors'] }) {
  const isIncome = item.type === 0;
  const freq = FREQUENCY_LABELS[item.frequency] ?? 'Monthly';
  const nextRun = item.nextRunDate ? new Date(item.nextRunDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }) : '—';

  return (
    <Card style={[styles.row, { borderColor: colors.border }]}>
      <View style={[styles.iconWrap, { backgroundColor: isIncome ? colors.success : colors.danger }]}>
        <Text style={styles.iconText}>{isIncome ? '💰' : '📤'}</Text>
      </View>
      <View style={styles.body}>
        <Text style={[styles.name, { color: colors.text.primary }]} numberOfLines={1}>
          {item.accountName || 'Account'} • {freq}
        </Text>
        <Text style={[styles.meta, { color: colors.text.muted }]}>
          Next: {nextRun}{item.note ? ` • ${item.note}` : ''}
        </Text>
        <Text style={[styles.amount, { color: isIncome ? colors.success : colors.danger }]}>
          {isIncome ? '+' : '−'}{Number(item.amount).toLocaleString()} {formatCurrencyCode(item.currency)}
        </Text>
      </View>
    </Card>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  list: { padding: 16 },
  empty: { marginTop: 24, padding: 24 },
  emptyTitle: { fontSize: 16, fontWeight: '600', marginBottom: 8 },
  emptyBody: { fontSize: 14 },
  row: { flexDirection: 'row', alignItems: 'center', padding: 14, marginBottom: 10 },
  iconWrap: { width: 44, height: 44, borderRadius: 12, justifyContent: 'center', alignItems: 'center', marginRight: 12 },
  iconText: { fontSize: 22 },
  body: { flex: 1, minWidth: 0 },
  name: { fontSize: 16, fontWeight: '600' },
  meta: { fontSize: 13, marginTop: 2 },
  amount: { fontSize: 16, fontWeight: '600', marginTop: 4 },
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
