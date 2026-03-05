import { View, Text, StyleSheet, FlatList, TouchableOpacity, RefreshControl } from 'react-native';
import { useRouter } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useTheme } from '../../../src/context/ThemeContext';
import { Card } from '../../../src/components/Card';
import { getAccounts } from '../../../src/api/accounts';
import { formatCurrencyCode } from '../../../src/utils/currency';

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

function accountTypeLabel(type: string | number): string {
  const key = String(type);
  return ACCOUNT_TYPE_LABELS[key] ?? (typeof type === 'string' ? type : 'Account');
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

  return (
    <View style={[styles.container, { backgroundColor: colors.bg.alt }]}>
      <FlatList
        data={active}
        keyExtractor={(item) => item.id}
        contentContainerStyle={[styles.listContent, { paddingBottom: insets.bottom + 88 }]}
        refreshControl={<RefreshControl refreshing={isRefetching} onRefresh={() => refetch()} tintColor={colors.brand} />}
        ListEmptyComponent={
          <Card style={styles.empty}>
            <Text style={[styles.emptyTitle, { color: colors.text.primary }]}>No accounts yet</Text>
            <Text style={[styles.emptyBody, { color: colors.text.muted }]}>Tap + to add an account</Text>
          </Card>
        }
        renderItem={({ item }) => (
          <TouchableOpacity onPress={() => router.push(`/(tabs)/accounts/${item.id}`)} activeOpacity={0.7}>
            <Card style={styles.row}>
              <Text style={[styles.rowName, { color: colors.text.primary }]}>{item.name}</Text>
              <Text style={[styles.rowType, { color: colors.text.muted }]}>
                {accountTypeLabel(item.type as string)}
              </Text>
              <Text style={[styles.rowBalance, { color: colors.text.primary }]}>
                {Number(item.currentBalance).toLocaleString()} {formatCurrencyCode(item.currency)}
              </Text>
            </Card>
          </TouchableOpacity>
        )}
      />
      <TouchableOpacity
        style={[styles.fab, { backgroundColor: colors.brand }]}
        onPress={() => router.push('/(tabs)/accounts/create')}
        activeOpacity={0.9}
      >
        <Text style={styles.fabText}>+</Text>
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  listContent: { padding: 16, paddingBottom: 88 },
  empty: { marginTop: 24 },
  emptyTitle: { fontSize: 16, fontWeight: '600', marginBottom: 8 },
  emptyBody: { fontSize: 14 },
  row: { marginBottom: 10 },
  rowName: { fontSize: 16, fontWeight: '600', marginBottom: 4 },
  rowType: { fontSize: 13 },
  rowBalance: { fontSize: 16, marginTop: 6 },
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
