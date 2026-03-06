import { useState } from 'react';
import { View, Text, StyleSheet, ScrollView, RefreshControl, TouchableOpacity } from 'react-native';
import { useRouter } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useTheme } from '../../src/context/ThemeContext';
import { useAuth } from '../../src/context/AuthContext';
import { Card } from '../../src/components/Card';
import { getDashboard } from '../../src/api/dashboard';
import { getUnreadCount } from '../../src/api/notifications';
import { LineChart } from 'react-native-chart-kit';
import { Dimensions } from 'react-native';

const DAYS = ['MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT', 'SUN'];

export default function DashboardScreen() {
  const { colors, isDark } = useTheme();
  const { user } = useAuth();
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const [userRefreshing, setUserRefreshing] = useState(false);
  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['dashboard'],
    queryFn: getDashboard,
    staleTime: 60 * 1000, // 1 min: avoid refetch when switching back to tab
  });
  const { data: unreadCount = 0 } = useQuery({
    queryKey: ['notificationsUnreadCount'],
    queryFn: getUnreadCount,
    staleTime: 60 * 1000,
  });

  const handleRefresh = async () => {
    setUserRefreshing(true);
    try {
      await refetch();
    } finally {
      setUserRefreshing(false);
    }
  };

  const chartConfig = {
    backgroundColor: colors.bg.default,
    backgroundGradientFrom: colors.bg.default,
    backgroundGradientTo: colors.bg.alt,
    decimalPlaces: 0,
    color: (opacity = 1) => (isDark ? `rgba(59, 130, 246, ${opacity})` : `rgba(37, 99, 235, ${opacity})`),
    labelColor: () => colors.text.muted,
    style: { borderRadius: 12, paddingRight: 0 },
  };

  const screenWidth = Dimensions.get('window').width - 48;
  const last7 = data?.chartData?.slice(-7) ?? [];
  const chartLabels = last7.length >= 7
    ? DAYS
    : last7.map((d, i) => (i % 2 === 0 ? d.date : ''));

  if (isError) {
    return (
      <View style={styles.wrapper}>
        <ScrollView
          style={[styles.container, { backgroundColor: colors.bg.alt }]}
          contentContainerStyle={[styles.centered, { paddingTop: insets.top + 16, paddingBottom: insets.bottom + 24 }]}
        >
        <Text style={[styles.errorText, { color: colors.danger }]}>
          {(error as Error)?.message ?? 'Failed to load dashboard'}
        </Text>
        <Text style={[styles.retryHint, { color: colors.text.muted }]}>Pull down to retry</Text>
        </ScrollView>
      </View>
    );
  }

  return (
    <View style={styles.wrapper}>
      <ScrollView
        style={[styles.container, { backgroundColor: colors.bg.alt }]}
        contentContainerStyle={[
          styles.content,
          { paddingTop: insets.top + 16, paddingBottom: insets.bottom + 24 },
        ]}
        refreshControl={
        <RefreshControl
          refreshing={userRefreshing}
          onRefresh={handleRefresh}
          tintColor={colors.brand}
        />
      }
    >
      {/* Header: profile (left) | Dashboard (center) | notifications (right) */}
      <View style={styles.header}>
        <View style={[styles.avatar, { backgroundColor: colors.brand }]}>
          <Text style={styles.avatarText}>
            {user?.firstName?.[0] ?? user?.email?.[0] ?? '?'}
          </Text>
        </View>
        <Text style={[styles.headerTitle, { color: colors.text.primary }]}>Dashboard</Text>
        <TouchableOpacity onPress={() => router.push('/(tabs)/notifications')} style={styles.bellWrap}>
          <Text style={styles.bellIcon}>🔔</Text>
          {unreadCount > 0 && (
            <View style={styles.bellBadge}>
              <Text style={styles.bellBadgeText} numberOfLines={1}>
                {unreadCount > 99 ? '99+' : unreadCount}
              </Text>
            </View>
          )}
        </TouchableOpacity>
      </View>

      {data ? (
        <>
          {/* This month's spending card (blue) — label matches the value shown */}
          <View style={[styles.balanceCard, { backgroundColor: colors.brand }]}>
            <Text style={styles.balanceLabel}>This month&apos;s spending</Text>
            <Text style={styles.balanceAmount}>
              {data.displayCurrency} {data.thisMonthSpend.toLocaleString()}
            </Text>
            <View style={styles.balanceRow}>
              <View style={styles.balanceCol}>
                <Text style={styles.balanceSubLabel}>Monthly expenses</Text>
                <Text style={styles.balanceSubValue}>−{data.thisMonthSpend.toLocaleString()}</Text>
              </View>
              <View style={[styles.balanceDivider, { backgroundColor: 'rgba(255,255,255,0.4)' }]} />
              <View style={styles.balanceCol}>
                <Text style={styles.balanceSubLabel}>Total spend (all time)</Text>
                <Text style={styles.balanceSubValue}>{data.totalSpend.toLocaleString()}</Text>
              </View>
            </View>
          </View>

          {/* Alerts */}
          {data.isOverBudget && data.budgetAmount != null && (
            <Card style={[styles.alert, { borderLeftWidth: 4, borderLeftColor: colors.danger }]}>
              <Text style={[styles.alertTitle, { color: colors.danger }]}>Budget exceeded</Text>
              <Text style={[styles.alertBody, { color: colors.text.body }]}>
                This month you've spent {data.thisMonthSpend.toLocaleString()} {data.displayCurrency} against a budget of {data.budgetAmount.toLocaleString()}.
              </Text>
            </Card>
          )}

          {/* Spending Trend */}
          <Card style={styles.trendCard}>
            <View style={styles.trendHeader}>
              <View>
                <Text style={[styles.trendTitle, { color: colors.text.primary }]}>Spending Trend</Text>
                <Text style={[styles.trendAmount, { color: colors.text.primary }]}>
                  {data.displayCurrency} {data.thisMonthSpend.toLocaleString()}
                </Text>
                <Text style={[styles.trendVs, { color: colors.danger }]}>— vs last week</Text>
              </View>
              <View style={[styles.trendPill, { backgroundColor: colors.brandLight ?? colors.bg.alt }]}>
                <Text style={[styles.trendPillText, { color: colors.brand }]}>Last 7 days</Text>
              </View>
            </View>
            {last7.length > 0 && (
              <LineChart
                data={{
                  labels: chartLabels,
                  datasets: [{ data: last7.map((d) => Math.max(0, Number(d.amount))) }],
                }}
                width={screenWidth}
                height={160}
                chartConfig={chartConfig}
                bezier
                style={{ marginLeft: -8 }}
                withDots={false}
                withInnerLines
                withOuterLines
                fromZero
              />
            )}
          </Card>

          {/* Active Budgets */}
          {(data.categoryBudgetAlerts?.length > 0 || (data.budgetAmount != null && data.budgetAmount > 0)) && (
            <View style={styles.section}>
              <View style={styles.sectionHeader}>
                <Text style={[styles.sectionTitle, { color: colors.text.primary }]}>Active Budgets</Text>
                <TouchableOpacity onPress={() => router.push('/(tabs)/budget')}>
                  <Text style={[styles.viewAll, { color: colors.brand }]}>View all</Text>
                </TouchableOpacity>
              </View>
              {data.budgetAmount != null && data.budgetAmount > 0 && (
                <Card style={styles.budgetItem}>
                  <View style={styles.budgetItemLeft}>
                    <Text style={styles.budgetEmoji}>📊</Text>
                    <View>
                      <Text style={[styles.budgetName, { color: colors.text.primary }]}>Total</Text>
                      <Text style={[styles.budgetMeta, { color: colors.text.muted }]}>
                        {data.thisMonthSpend.toLocaleString()} / {data.budgetAmount.toLocaleString()} {data.displayCurrency}
                      </Text>
                      <View style={[styles.progressTrack, { backgroundColor: colors.border }]}>
                        <View
                          style={[
                            styles.progressFill,
                            {
                              width: `${Math.min(100, (data.thisMonthSpend / data.budgetAmount) * 100)}%`,
                              backgroundColor: data.isOverBudget ? colors.danger : colors.brand,
                            },
                          ]}
                        />
                      </View>
                    </View>
                  </View>
                </Card>
              )}
              {data.categoryBudgetAlerts?.slice(0, 2).map((a, i) => (
                <Card key={i} style={styles.budgetItem}>
                  <View style={styles.budgetItemLeft}>
                    <Text style={styles.budgetEmoji}>{a.isOver ? '⚠️' : '📌'}</Text>
                    <View>
                      <Text style={[styles.budgetName, { color: colors.text.primary }]}>{a.categoryName}</Text>
                      <Text style={[styles.budgetMeta, { color: colors.text.muted }]}>
                        {a.spent.toLocaleString()} / {a.budget.toLocaleString()} {a.currency}
                      </Text>
                      <View style={[styles.progressTrack, { backgroundColor: colors.border }]}>
                        <View
                          style={[
                            styles.progressFill,
                            {
                              width: `${Math.min(100, (a.spent / a.budget) * 100)}%`,
                              backgroundColor: a.isOver ? colors.danger : colors.warning,
                            },
                          ]}
                        />
                      </View>
                    </View>
                  </View>
                </Card>
              ))}
            </View>
          )}

          {/* Recent — link to expenses */}
          <View style={styles.section}>
            <View style={styles.sectionHeader}>
              <Text style={[styles.sectionTitle, { color: colors.text.primary }]}>Recent Transactions</Text>
              <TouchableOpacity onPress={() => router.push('/(tabs)/expenses')}>
                <Text style={[styles.viewAll, { color: colors.brand }]}>See all</Text>
              </TouchableOpacity>
            </View>
            <Card style={styles.recentCard}>
              <Text style={[styles.recentHint, { color: colors.text.muted }]}>
                Your latest expenses appear on the Expenses tab.
              </Text>
              <TouchableOpacity
                style={[styles.recentBtn, { backgroundColor: colors.brandLight ?? colors.bg.alt }]}
                onPress={() => router.push('/(tabs)/expenses')}
              >
                <Text style={[styles.recentBtnText, { color: colors.brand }]}>Open Expenses</Text>
              </TouchableOpacity>
            </Card>
          </View>
        </>
      ) : isLoading ? (
        <View style={styles.loadingWrap}>
          <Text style={[styles.loadingText, { color: colors.text.muted }]}>Loading...</Text>
        </View>
      ) : null}
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  wrapper: { flex: 1 },
  container: { flex: 1 },
  content: { padding: 16 },
  centered: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 24 },
  errorText: { fontSize: 16, marginBottom: 8, textAlign: 'center' },
  retryHint: { fontSize: 14 },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 20,
  },
  avatar: {
    width: 40,
    height: 40,
    borderRadius: 20,
    justifyContent: 'center',
    alignItems: 'center',
  },
  avatarText: { color: '#fff', fontSize: 18, fontWeight: '700' },
  headerTitle: { flex: 1, fontSize: 20, fontWeight: '700', textAlign: 'center' },
  bellWrap: { padding: 8, position: 'relative' },
  bellIcon: { fontSize: 22 },
  bellBadge: {
    position: 'absolute',
    top: 0,
    right: 0,
    minWidth: 18,
    height: 18,
    borderRadius: 9,
    backgroundColor: '#DC2626',
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 5,
  },
  bellBadgeText: { color: '#fff', fontSize: 11, fontWeight: '700' },
  balanceCard: {
    borderRadius: 16,
    padding: 20,
    marginBottom: 16,
  },
  balanceLabel: { color: 'rgba(255,255,255,0.9)', fontSize: 14, marginBottom: 6 },
  balanceAmount: { color: '#fff', fontSize: 28, fontWeight: '700', marginBottom: 16 },
  balanceRow: { flexDirection: 'row', alignItems: 'center' },
  balanceCol: { flex: 1 },
  balanceDivider: { width: 1, height: 32 },
  balanceSubLabel: { color: 'rgba(255,255,255,0.8)', fontSize: 12 },
  balanceSubValue: { color: '#fff', fontSize: 15, fontWeight: '600' },
  alert: { marginBottom: 12 },
  alertTitle: { fontSize: 15, fontWeight: '600', marginBottom: 4 },
  alertBody: { fontSize: 14 },
  trendCard: { marginBottom: 20 },
  trendHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 12 },
  trendTitle: { fontSize: 16, fontWeight: '700', marginBottom: 4 },
  trendAmount: { fontSize: 22, fontWeight: '700' },
  trendVs: { fontSize: 13, marginTop: 2 },
  trendPill: { paddingHorizontal: 12, paddingVertical: 6, borderRadius: 8 },
  trendPillText: { fontSize: 13, fontWeight: '600' },
  section: { marginBottom: 20 },
  sectionHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 },
  sectionTitle: { fontSize: 16, fontWeight: '700' },
  viewAll: { fontSize: 14, fontWeight: '600' },
  budgetItem: { marginBottom: 10 },
  budgetItemLeft: { flexDirection: 'row', alignItems: 'center' },
  budgetEmoji: { fontSize: 24, marginRight: 12 },
  budgetName: { fontSize: 16, fontWeight: '600' },
  budgetMeta: { fontSize: 13, marginTop: 2 },
  progressTrack: { height: 6, borderRadius: 3, marginTop: 8, overflow: 'hidden' },
  progressFill: { height: '100%', borderRadius: 3 },
  recentCard: { padding: 16 },
  recentHint: { fontSize: 14, marginBottom: 12 },
  recentBtn: { alignSelf: 'flex-start', paddingHorizontal: 16, paddingVertical: 10, borderRadius: 10 },
  recentBtnText: { fontSize: 14, fontWeight: '600' },
  loadingWrap: { padding: 24, alignItems: 'center' },
  loadingText: { fontSize: 15 },
});
