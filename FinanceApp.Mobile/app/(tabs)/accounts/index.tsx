import { View, Text, StyleSheet, FlatList, TouchableOpacity, RefreshControl } from 'react-native';
import { useRouter } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useTheme } from '../../../src/context/ThemeContext';
import { Card } from '../../../src/components/Card';
import { Icon, type IconName } from '../../../src/components/Icon';
import { getAccounts } from '../../../src/api/accounts';
import { formatCurrencyCode } from '../../../src/utils/currency';
import type { AccountDto } from '../../../src/types/api';

const ACCOUNT_TYPE_LABELS: Record<string, string> = {
  '0': 'Checking',
  '1': 'Savings',
  '2': 'Credit Card',
  '3': 'Cash',
  '4': 'Investment',
  Checking: 'Checking',
  Savings: 'Savings',
  CreditCard: 'Credit Card',
  Cash: 'Cash',
  Investment: 'Investment',
};

const ACCOUNT_TYPE_ICONS: Record<string, IconName> = {
  '0': 'accounts',
  '1': 'accountSavings',
  '2': 'accountCreditCard',
  '3': 'accountCash',
  '4': 'accountInvestment',
  Checking: 'accounts',
  Savings: 'accountSavings',
  CreditCard: 'accountCreditCard',
  Cash: 'accountCash',
  Investment: 'accountInvestment',
};

function accountTypeLabel(type: string): string {
  return ACCOUNT_TYPE_LABELS[type] ?? type;
}

function accountTypeIcon(type: string): IconName {
  return ACCOUNT_TYPE_ICONS[type] ?? 'accounts';
}

/** Balances only sum meaningfully within one currency — grouped so a mixed
 *  portfolio doesn't get silently added together into a false total. */
function groupBalancesByCurrency(accounts: AccountDto[]): { currency: string; total: number }[] {
  const totals = new Map<string, number>();
  accounts.forEach((a) => {
    totals.set(a.currency, (totals.get(a.currency) ?? 0) + Number(a.currentBalance));
  });
  return [...totals.entries()]
    .map(([currency, total]) => ({ currency, total }))
    .sort((a, b) => b.total - a.total);
}

export default function AccountsListScreen() {
  const { colors } = useTheme();
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { data: accounts = [], refetch, isRefetching } = useQuery({
    queryKey: ['accounts'],
    queryFn: getAccounts,
  });
  const active = accounts.filter((a) => a.isActive);
  const balancesByCurrency = groupBalancesByCurrency(active);

  return (
    <View style={[styles.container, { backgroundColor: colors.bg.alt }]}>
      <FlatList
        data={active}
        keyExtractor={(item) => item.id}
        contentContainerStyle={[styles.listContent, { paddingBottom: insets.bottom + 88 }]}
        refreshControl={<RefreshControl refreshing={isRefetching} onRefresh={() => refetch()} tintColor={colors.brand} />}
        ListHeaderComponent={
          active.length > 0 ? (
            <View style={styles.summaryRow}>
              {balancesByCurrency.map(({ currency, total }) => (
                <Card key={currency} style={styles.summaryCard}>
                  <Text style={[styles.summaryLabel, { color: colors.text.muted }]}>
                    TOTAL {formatCurrencyCode(currency)}
                  </Text>
                  <Text style={[styles.summaryValue, { color: total < 0 ? colors.danger : colors.text.primary }]}>
                    {total.toLocaleString()}
                  </Text>
                </Card>
              ))}
            </View>
          ) : null
        }
        ListEmptyComponent={
          <Card style={styles.empty}>
            <View style={[styles.emptyIconWrap, { backgroundColor: colors.bg.hover }]}>
              <Icon name="accounts" size={22} color={colors.text.subtle} />
            </View>
            <Text style={[styles.emptyTitle, { color: colors.text.primary }]}>No accounts yet</Text>
            <Text style={[styles.emptyBody, { color: colors.text.muted }]}>
              Add a bank, cash, or card account to track its balance here.
            </Text>
          </Card>
        }
        renderItem={({ item }) => (
          <TouchableOpacity onPress={() => router.push(`/(tabs)/accounts/${item.id}`)} activeOpacity={0.7}>
            <Card style={styles.row}>
              <View style={[styles.rowIcon, { backgroundColor: colors.brandLight }]}>
                <Icon name={accountTypeIcon(item.type)} size={20} color={colors.brand} />
              </View>
              <View style={styles.rowBody}>
                <Text style={[styles.rowName, { color: colors.text.primary }]} numberOfLines={1}>
                  {item.name}
                </Text>
                <Text style={[styles.rowType, { color: colors.text.muted }]}>
                  {accountTypeLabel(item.type)}
                </Text>
              </View>
              <Text
                style={[
                  styles.rowBalance,
                  { color: Number(item.currentBalance) < 0 ? colors.danger : colors.text.primary },
                ]}
              >
                {Number(item.currentBalance).toLocaleString()}
                <Text style={[styles.rowBalanceCode, { color: colors.text.muted }]}> {formatCurrencyCode(item.currency)}</Text>
              </Text>
            </Card>
          </TouchableOpacity>
        )}
      />
      <TouchableOpacity
        style={[styles.fab, { backgroundColor: colors.brand }]}
        onPress={() => router.push('/(tabs)/accounts/create')}
        activeOpacity={0.9}
        accessibilityLabel="Add account"
      >
        <Icon name="add" size={26} color={colors.brandContrast} />
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  listContent: { padding: 16, paddingBottom: 88 },
  summaryRow: { flexDirection: 'row', flexWrap: 'wrap', gap: 10, marginBottom: 12 },
  summaryCard: { flex: 1, minWidth: 140, paddingVertical: 14 },
  summaryLabel: { fontSize: 11, fontWeight: '700', letterSpacing: 0.5, marginBottom: 6 },
  summaryValue: { fontSize: 20, fontWeight: '700' },
  empty: { marginTop: 24, alignItems: 'center', paddingVertical: 32 },
  emptyIconWrap: { width: 48, height: 48, borderRadius: 24, justifyContent: 'center', alignItems: 'center', marginBottom: 12 },
  emptyTitle: { fontSize: 16, fontWeight: '600', marginBottom: 6 },
  emptyBody: { fontSize: 14, textAlign: 'center', maxWidth: 260 },
  row: { flexDirection: 'row', alignItems: 'center', marginBottom: 10 },
  rowIcon: { width: 44, height: 44, borderRadius: 22, justifyContent: 'center', alignItems: 'center', marginRight: 12 },
  rowBody: { flex: 1, minWidth: 0 },
  rowName: { fontSize: 16, fontWeight: '600', marginBottom: 2 },
  rowType: { fontSize: 13 },
  rowBalance: { fontSize: 16, fontWeight: '700' },
  rowBalanceCode: { fontSize: 12, fontWeight: '500' },
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
});
