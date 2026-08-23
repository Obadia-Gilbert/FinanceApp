import { useState, useRef, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  RefreshControl,
  TouchableOpacity,
  ActivityIndicator,
  Animated,
} from 'react-native';
import { useRouter } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useTheme } from '../../src/context/ThemeContext';
import { useAuth } from '../../src/context/AuthContext';
import { Card } from '../../src/components/Card';
import { Icon } from '../../src/components/Icon';
import { getDashboard } from '../../src/api/dashboard';
import { getUnreadCount } from '../../src/api/notifications';
import { getProfile } from '../../src/api/profile';
import { ProfileAvatar } from '../../src/components/ProfileAvatar';
import { formatAmount } from '../../src/utils/currency';
import { LineChart } from 'react-native-chart-kit';
import { Dimensions } from 'react-native';

const WEEKDAY_KEYS = ['mon', 'tue', 'wed', 'thu', 'fri', 'sat', 'sun'] as const;

export default function DashboardScreen() {
  const { t } = useTranslation();
  const { colors, isDark } = useTheme();
  const { user } = useAuth();
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const [userRefreshing, setUserRefreshing] = useState(false);
  const fadeIn = useRef(new Animated.Value(0)).current;

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['dashboard'],
    queryFn: getDashboard,
    staleTime: 60 * 1000,
  });
  const { data: unreadCount = 0 } = useQuery({
    queryKey: ['notificationsUnreadCount'],
    queryFn: getUnreadCount,
    staleTime: 60 * 1000,
  });
  // Only for the header avatar — the stored auth user has no image path.
  const { data: profile } = useQuery({
    queryKey: ['profile'],
    queryFn: getProfile,
    staleTime: 5 * 60 * 1000,
  });

  useEffect(() => {
    if (data) {
      Animated.timing(fadeIn, { toValue: 1, duration: 400, useNativeDriver: true }).start();
    }
  }, [data]);

  const handleRefresh = async () => {
    setUserRefreshing(true);
    try { await refetch(); } finally { setUserRefreshing(false); }
  };

  const chartConfig = {
    backgroundColor: colors.bg.default,
    backgroundGradientFrom: colors.bg.default,
    backgroundGradientTo: colors.bg.alt,
    decimalPlaces: 0,
    // Brand teal (colors.brand), not chart-kit's default blue — #2DD4BF dark / #0D9488 light.
    color: (opacity = 1) => isDark ? `rgba(45, 212, 191, ${opacity})` : `rgba(13, 148, 136, ${opacity})`,
    labelColor: () => colors.text.muted,
    style: { borderRadius: 12, paddingRight: 0 },
  };

  const screenWidth = Dimensions.get('window').width - 48;
  const last7 = data?.chartData?.slice(-7) ?? [];
  const chartLabels = last7.length >= 7
    ? WEEKDAY_KEYS.map((k) => t(`dashboard.weekdays.${k}`))
    : last7.map((d, i) => (i % 2 === 0 ? d.date : ''));

  if (isError) {
    return (
      <View style={styles.wrapper}>
        <ScrollView
          style={[styles.container, { backgroundColor: colors.bg.alt }]}
          contentContainerStyle={[styles.centered, { paddingTop: insets.top + 16, paddingBottom: insets.bottom + 24 }]}
          refreshControl={<RefreshControl refreshing={false} onRefresh={() => refetch()} tintColor={colors.brand} />}
        >
          <Icon name="error" size={44} color={colors.text.subtle} style={{ marginBottom: 16 }} />
          <Text style={[styles.errorText, { color: colors.danger }]}>
            {(error as Error)?.message ?? t('dashboard.loadFailed')}
          </Text>
          <Text style={[styles.retryHint, { color: colors.text.muted }]}>{t('dashboard.pullToRetry')}</Text>
        </ScrollView>
      </View>
    );
  }

  return (
    <View style={styles.wrapper}>
      <ScrollView
        style={[styles.container, { backgroundColor: colors.bg.alt }]}
        contentContainerStyle={[styles.content, { paddingTop: insets.top + 16, paddingBottom: insets.bottom + 24 }]}
        refreshControl={
          <RefreshControl refreshing={userRefreshing} onRefresh={handleRefresh} tintColor={colors.brand} />
        }
      >
        {/* Header */}
        <View style={styles.header}>
          <TouchableOpacity
            onPress={() => router.push('/(tabs)/profile')}
            activeOpacity={0.8}
          >
            <ProfileAvatar
              hasImage={!!profile?.profileImagePath}
              initial={user?.firstName?.[0] ?? user?.email?.[0] ?? '?'}
              size={44}
              cacheKey={profile?.profileImagePath ?? undefined}
            />
          </TouchableOpacity>
          <Text style={[styles.headerTitle, { color: colors.text.primary }]}>{t('dashboard.title')}</Text>
          <TouchableOpacity
            onPress={() => router.push('/(tabs)/notifications')}
            style={styles.bellWrap}
            activeOpacity={0.7}
          >
            <Icon name="notifications" size={22} color={colors.text.body} />
            {unreadCount > 0 && (
              <View style={[styles.bellBadge, { backgroundColor: colors.danger }]}>
                {/* See HeaderNotificationIcon: no ellipsis, or multi-digit
                    counts render as "1..". */}
                <Text style={styles.bellBadgeText}>
                  {unreadCount > 99 ? '99+' : unreadCount}
                </Text>
              </View>
            )}
          </TouchableOpacity>
        </View>

        {data ? (
          <Animated.View style={{ opacity: fadeIn }}>
            {/* Spending card */}
            <View style={[styles.balanceCard, { backgroundColor: colors.brand }]}>
              <Text style={styles.balanceLabel}>{t('dashboard.thisMonthSpending')}</Text>
              <Text style={styles.balanceAmount}>
                {data.displayCurrency} {formatAmount(data.thisMonthSpend, data.displayCurrency)}
              </Text>
              <View style={styles.balanceRow}>
                <View style={styles.balanceCol}>
                  <Text style={styles.balanceSubLabel}>{t('dashboard.monthlyExpenses')}</Text>
                  <Text style={styles.balanceSubValue}>−{formatAmount(data.thisMonthSpend, data.displayCurrency)}</Text>
                </View>
                <View style={styles.balanceDivider} />
                <View style={styles.balanceCol}>
                  <Text style={styles.balanceSubLabel}>{t('dashboard.totalSpendAllTime')}</Text>
                  <Text style={styles.balanceSubValue}>{formatAmount(data.totalSpend, data.displayCurrency)}</Text>
                </View>
              </View>
            </View>

            {/* Budget alert */}
            {data.isOverBudget && data.budgetAmount != null && (
              <Card style={[styles.alert, { borderLeftWidth: 4, borderLeftColor: colors.danger }]}>
                <View style={styles.alertTitleRow}>
                  <Icon name="warning" size={16} color={colors.danger} />
                  <Text style={[styles.alertTitle, { color: colors.danger }]}>
                    {t('dashboard.budgetExceeded')}
                  </Text>
                </View>
                <Text style={[styles.alertBody, { color: colors.text.body }]}>
                  {t('dashboard.budgetExceededBody', {
                    spent: formatAmount(data.thisMonthSpend, data.displayCurrency),
                    currency: data.displayCurrency,
                    budget: formatAmount(data.budgetAmount, data.displayCurrency),
                  })}
                </Text>
              </Card>
            )}

            {/* Spending Trend */}
            {last7.length > 0 && (
              <Card style={styles.trendCard}>
                <View style={styles.trendHeader}>
                  <View>
                    <Text style={[styles.trendTitle, { color: colors.text.primary }]}>{t('dashboard.spendingTrend')}</Text>
                    <Text style={[styles.trendAmount, { color: colors.text.primary }]}>
                      {data.displayCurrency} {formatAmount(data.thisMonthSpend, data.displayCurrency)}
                    </Text>
                  </View>
                  <View style={[styles.trendPill, { backgroundColor: colors.brandLight ?? colors.bg.alt }]}>
                    <Text style={[styles.trendPillText, { color: colors.brand }]}>{t('dashboard.last7Days')}</Text>
                  </View>
                </View>
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
              </Card>
            )}

            {/* Active Budgets */}
            {(data.categoryBudgetAlerts?.length > 0 || (data.budgetAmount != null && data.budgetAmount > 0)) && (
              <View style={styles.section}>
                <View style={styles.sectionHeader}>
                  <Text style={[styles.sectionTitle, { color: colors.text.primary }]}>{t('dashboard.activeBudgets')}</Text>
                  <TouchableOpacity onPress={() => router.push('/(tabs)/budget')}>
                    <Text style={[styles.viewAll, { color: colors.brand }]}>{t('dashboard.viewAll')}</Text>
                  </TouchableOpacity>
                </View>
                {data.budgetAmount != null && data.budgetAmount > 0 && (
                  <Card style={styles.budgetItem}>
                    <View style={styles.budgetItemLeft}>
                      <View style={[styles.budgetIconWrap, { backgroundColor: colors.brandLight ?? colors.bg.alt }]}>
                        <Icon name="budget" size={20} color={colors.brand} />
                      </View>
                      <View style={{ flex: 1 }}>
                        <Text style={[styles.budgetName, { color: colors.text.primary }]}>{t('dashboard.total')}</Text>
                        <Text style={[styles.budgetMeta, { color: colors.text.muted }]}>
                          {formatAmount(data.thisMonthSpend, data.displayCurrency)} / {formatAmount(data.budgetAmount, data.displayCurrency)} {data.displayCurrency}
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
                {data.categoryBudgetAlerts?.slice(0, 3).map((a, i) => (
                  <Card key={i} style={styles.budgetItem}>
                    <View style={styles.budgetItemLeft}>
                      <View style={[styles.budgetIconWrap, { backgroundColor: a.isOver ? `${colors.danger}20` : `${colors.warning}20` }]}>
                        <Icon name="warning" size={20} color={a.isOver ? colors.danger : colors.warning} />
                      </View>
                      <View style={{ flex: 1 }}>
                        <Text style={[styles.budgetName, { color: colors.text.primary }]}>{a.categoryName}</Text>
                        <Text style={[styles.budgetMeta, { color: colors.text.muted }]}>
                          {formatAmount(a.spent, a.currency)} / {formatAmount(a.budget, a.currency)} {a.currency}
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

            {/* Quick Actions */}
            <View style={styles.section}>
              <Text style={[styles.sectionTitle, { color: colors.text.primary, marginBottom: 12 }]}>{t('dashboard.quickActions')}</Text>
              <View style={styles.quickActions}>
                <TouchableOpacity
                  style={[styles.quickAction, { backgroundColor: colors.bg.default, borderColor: colors.border }]}
                  onPress={() => router.push(`/(tabs)/expenses/create?t=${Date.now()}`)}
                  activeOpacity={0.7}
                >
                  <View style={[styles.quickActionIcon, { backgroundColor: colors.bg.alt }]}>
                    <Icon name="expense" size={20} color={colors.text.body} />
                  </View>
                  <Text style={[styles.quickActionLabel, { color: colors.text.primary }]}>{t('dashboard.addExpense')}</Text>
                </TouchableOpacity>
                <TouchableOpacity
                  style={[styles.quickAction, { backgroundColor: colors.bg.default, borderColor: colors.border }]}
                  onPress={() => router.push('/(tabs)/income/create')}
                  activeOpacity={0.7}
                >
                  <View style={[styles.quickActionIcon, { backgroundColor: colors.bg.alt }]}>
                    <Icon name="income" size={20} color={colors.text.body} />
                  </View>
                  <Text style={[styles.quickActionLabel, { color: colors.text.primary }]}>{t('dashboard.addIncome')}</Text>
                </TouchableOpacity>
                <TouchableOpacity
                  style={[styles.quickAction, { backgroundColor: colors.bg.default, borderColor: colors.border }]}
                  onPress={() => router.push('/(tabs)/reports')}
                  activeOpacity={0.7}
                >
                  <View style={[styles.quickActionIcon, { backgroundColor: colors.bg.alt }]}>
                    <Icon name="report" size={20} color={colors.text.body} />
                  </View>
                  <Text style={[styles.quickActionLabel, { color: colors.text.primary }]}>{t('dashboard.reports')}</Text>
                </TouchableOpacity>
                <TouchableOpacity
                  style={[styles.quickAction, { backgroundColor: colors.bg.default, borderColor: colors.border }]}
                  onPress={() => router.push('/(tabs)/budget')}
                  activeOpacity={0.7}
                >
                  <View style={[styles.quickActionIcon, { backgroundColor: colors.bg.alt }]}>
                    <Icon name="budget" size={20} color={colors.text.body} />
                  </View>
                  <Text style={[styles.quickActionLabel, { color: colors.text.primary }]}>{t('dashboard.budget')}</Text>
                </TouchableOpacity>
              </View>
            </View>
          </Animated.View>
        ) : isLoading ? (
          <View style={styles.loadingWrap}>
            <ActivityIndicator size="large" color={colors.brand} />
            <Text style={[styles.loadingText, { color: colors.text.muted }]}>{t('dashboard.loading')}</Text>
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
    width: 42,
    height: 42,
    borderRadius: 21,
    justifyContent: 'center',
    alignItems: 'center',
  },
  avatarText: { fontSize: 18, fontWeight: '700' },
  headerTitle: { flex: 1, fontSize: 20, fontWeight: '700', textAlign: 'center' },
  bellWrap: { padding: 8, position: 'relative' },
  bellBadge: {
    position: 'absolute',
    top: 0,
    right: -2,
    minWidth: 18,
    height: 18,
    borderRadius: 9,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 5,
  },
  bellBadgeText: {
    color: '#fff',
    fontSize: 11,
    fontWeight: '700',
    flexShrink: 0,
    textAlign: 'center',
  },
  balanceCard: {
    borderRadius: 16,
    padding: 20,
    marginBottom: 16,
  },
  balanceLabel: { color: 'rgba(255,255,255,0.9)', fontSize: 14, marginBottom: 6 },
  balanceAmount: { color: '#fff', fontSize: 28, fontWeight: '700', marginBottom: 16 },
  balanceRow: { flexDirection: 'row', alignItems: 'center' },
  balanceCol: { flex: 1 },
  balanceDivider: { width: 1, height: 32, backgroundColor: 'rgba(255,255,255,0.3)' },
  balanceSubLabel: { color: 'rgba(255,255,255,0.8)', fontSize: 12 },
  balanceSubValue: { color: '#fff', fontSize: 15, fontWeight: '600' },
  alert: { marginBottom: 12 },
  alertTitleRow: { flexDirection: 'row', alignItems: 'center', gap: 6, marginBottom: 4 },
  alertTitle: { fontSize: 15, fontWeight: '600' },
  alertBody: { fontSize: 14 },
  trendCard: { marginBottom: 20 },
  trendHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 12 },
  trendTitle: { fontSize: 16, fontWeight: '700', marginBottom: 4 },
  trendAmount: { fontSize: 22, fontWeight: '700' },
  trendPill: { paddingHorizontal: 12, paddingVertical: 6, borderRadius: 8 },
  trendPillText: { fontSize: 13, fontWeight: '600' },
  section: { marginBottom: 20 },
  sectionHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 },
  sectionTitle: { fontSize: 16, fontWeight: '700' },
  viewAll: { fontSize: 14, fontWeight: '600' },
  budgetItem: { marginBottom: 10 },
  budgetItemLeft: { flexDirection: 'row', alignItems: 'center' },
  budgetIconWrap: {
    width: 44,
    height: 44,
    borderRadius: 22,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  budgetName: { fontSize: 16, fontWeight: '600' },
  budgetMeta: { fontSize: 13, marginTop: 2 },
  progressTrack: { height: 6, borderRadius: 3, marginTop: 8, overflow: 'hidden' },
  progressFill: { height: '100%', borderRadius: 3 },
  quickActions: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 10,
  },
  quickAction: {
    width: '48%',
    flexDirection: 'row',
    alignItems: 'center',
    padding: 14,
    borderRadius: 12,
    borderWidth: 1,
    gap: 10,
  },
  quickActionIcon: {
    width: 40,
    height: 40,
    borderRadius: 12,
    justifyContent: 'center',
    alignItems: 'center',
  },
  quickActionLabel: { fontSize: 14, fontWeight: '600', flex: 1 },
  loadingWrap: { padding: 60, alignItems: 'center', gap: 16 },
  loadingText: { fontSize: 15 },
});
