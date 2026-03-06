import { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  RefreshControl,
  ActivityIndicator,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { useTheme } from '../../src/context/ThemeContext';
import { Card } from '../../src/components/Card';
import { getMonthlyReport } from '../../src/api/reports';

const MONTHS = 'Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov Dec'.split(' ');

export default function ReportsScreen() {
  const { colors } = useTheme();
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);

  const { data: report, isLoading, refetch, isRefetching } = useQuery({
    queryKey: ['report', year, month],
    queryFn: () => getMonthlyReport(year, month),
  });

  return (
    <ScrollView
      style={[styles.container, { backgroundColor: colors.bg.alt }]}
      contentContainerStyle={styles.content}
      refreshControl={<RefreshControl refreshing={isRefetching && !isLoading} onRefresh={() => refetch()} tintColor={colors.brand} />}
    >
      <Text style={[styles.title, { color: colors.text.primary }]}>Monthly Report</Text>

      {/* Month picker */}
      <View style={styles.pickerRow}>
        <ScrollView horizontal showsHorizontalScrollIndicator={false}>
          {MONTHS.map((m, i) => (
            <TouchableOpacity
              key={m}
              onPress={() => setMonth(i + 1)}
              style={[
                styles.monthChip,
                {
                  backgroundColor: month === i + 1 ? colors.brand : colors.bg.default,
                  borderColor: month === i + 1 ? colors.brand : colors.border,
                },
              ]}
            >
              <Text style={[styles.monthChipText, { color: month === i + 1 ? '#fff' : colors.text.body }]}>{m}</Text>
            </TouchableOpacity>
          ))}
        </ScrollView>
      </View>

      {/* Year */}
      <View style={styles.yearWrap}>
        <TouchableOpacity
          onPress={() => setYear((y) => y - 1)}
          style={[styles.arrowBtn, { backgroundColor: colors.bg.default, borderColor: colors.border }]}
        >
          <Text style={[{ fontSize: 18, color: colors.text.body }]}>‹</Text>
        </TouchableOpacity>
        <Text style={[styles.yearText, { color: colors.text.primary }]}>{year}</Text>
        <TouchableOpacity
          onPress={() => setYear((y) => y + 1)}
          style={[styles.arrowBtn, { backgroundColor: colors.bg.default, borderColor: colors.border }]}
        >
          <Text style={[{ fontSize: 18, color: colors.text.body }]}>›</Text>
        </TouchableOpacity>
      </View>

      {isLoading ? (
        <View style={styles.loadingWrap}>
          <ActivityIndicator size="large" color={colors.brand} />
        </View>
      ) : report ? (
        <>
          {/* Summary cards */}
          <View style={styles.summaryRow}>
            <Card style={[styles.summaryCard, { flex: 1 }]}>
              <Text style={[styles.summaryLabel, { color: colors.text.muted }]}>Spent</Text>
              <Text style={[styles.summaryValue, { color: colors.danger }]}>
                {report.totalSpent.toLocaleString()}
              </Text>
              <Text style={[styles.summaryCurrency, { color: colors.text.subtle }]}>{report.currency}</Text>
            </Card>
            <Card style={[styles.summaryCard, { flex: 1 }]}>
              <Text style={[styles.summaryLabel, { color: colors.text.muted }]}>Income</Text>
              <Text style={[styles.summaryValue, { color: colors.success }]}>
                {report.totalIncome.toLocaleString()}
              </Text>
              <Text style={[styles.summaryCurrency, { color: colors.text.subtle }]}>{report.currency}</Text>
            </Card>
          </View>

          {/* Net cash flow */}
          <Card style={styles.cashFlowCard}>
            <View style={styles.cashFlowRow}>
              <Text style={[styles.cashFlowLabel, { color: colors.text.primary }]}>Net Cash Flow</Text>
              <Text style={[styles.cashFlowValue, { color: report.netCashFlow >= 0 ? colors.success : colors.danger }]}>
                {report.netCashFlow >= 0 ? '+' : ''}{report.netCashFlow.toLocaleString()} {report.currency}
              </Text>
            </View>
            {report.globalBudgetAmount != null && (
              <View style={[styles.budgetRow, { borderTopColor: colors.border }]}>
                <Text style={[styles.statLabel, { color: colors.text.muted }]}>Budget</Text>
                <Text style={[styles.statValue, { color: colors.text.primary }]}>
                  {report.globalBudgetSpent?.toLocaleString() ?? 0} / {report.globalBudgetAmount.toLocaleString()} {report.currency}
                </Text>
              </View>
            )}
          </Card>

          {/* Category breakdown */}
          {report.categoryLines && report.categoryLines.length > 0 && (
            <Card style={styles.card}>
              <Text style={[styles.cardTitle, { color: colors.text.primary }]}>By Category</Text>
              {report.categoryLines.slice(0, 10).map((line, i) => {
                const maxSpent = Math.max(...report.categoryLines!.map((l) => l.spent));
                const barWidth = maxSpent > 0 ? Math.max(4, (line.spent / maxSpent) * 100) : 0;
                return (
                  <View key={i} style={styles.catRow}>
                    <View style={styles.catInfo}>
                      <Text style={[styles.catName, { color: colors.text.primary }]} numberOfLines={1}>{line.categoryName}</Text>
                      <Text style={[styles.catAmount, { color: line.isOverBudget ? colors.danger : colors.text.muted }]}>
                        {line.spent.toLocaleString()} {report.currency}
                      </Text>
                    </View>
                    <View style={[styles.catBar, { backgroundColor: colors.border }]}>
                      <View style={[styles.catBarFill, { width: `${barWidth}%`, backgroundColor: line.isOverBudget ? colors.danger : colors.brand }]} />
                    </View>
                  </View>
                );
              })}
            </Card>
          )}

          {/* Top expenses */}
          {report.topExpenses && report.topExpenses.length > 0 && (
            <Card style={styles.card}>
              <Text style={[styles.cardTitle, { color: colors.text.primary }]}>Top Expenses</Text>
              {report.topExpenses.slice(0, 5).map((exp, i) => (
                <View key={i} style={[styles.topRow, { borderBottomColor: colors.border }]}>
                  <View style={[styles.topRank, { backgroundColor: `${colors.brand}15` }]}>
                    <Text style={[styles.topRankText, { color: colors.brand }]}>{i + 1}</Text>
                  </View>
                  <Text style={[styles.topName, { color: colors.text.primary }]} numberOfLines={1}>
                    {exp.description || exp.categoryName}
                  </Text>
                  <Text style={[styles.topAmount, { color: colors.text.primary }]}>
                    {exp.amount.toLocaleString()} {exp.currency}
                  </Text>
                </View>
              ))}
            </Card>
          )}
        </>
      ) : (
        <View style={styles.emptyWrap}>
          <Text style={{ fontSize: 48, marginBottom: 12 }}>📊</Text>
          <Text style={[styles.emptyTitle, { color: colors.text.primary }]}>No data for {MONTHS[month - 1]} {year}</Text>
          <Text style={[styles.emptyBody, { color: colors.text.muted }]}>Start tracking expenses to see your report.</Text>
        </View>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 16, paddingBottom: 40 },
  title: { fontSize: 22, fontWeight: '700', marginBottom: 16 },
  pickerRow: { marginBottom: 12 },
  monthChip: { paddingHorizontal: 14, paddingVertical: 10, borderRadius: 10, marginRight: 8, borderWidth: 1 },
  monthChipText: { fontSize: 14, fontWeight: '500' },
  yearWrap: { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 16, marginBottom: 20 },
  arrowBtn: { width: 36, height: 36, borderRadius: 10, borderWidth: 1, justifyContent: 'center', alignItems: 'center' },
  yearText: { fontSize: 18, fontWeight: '700', minWidth: 48, textAlign: 'center' },
  loadingWrap: { padding: 60, alignItems: 'center' },
  summaryRow: { flexDirection: 'row', gap: 12, marginBottom: 12 },
  summaryCard: { alignItems: 'center', paddingVertical: 20 },
  summaryLabel: { fontSize: 12, fontWeight: '600', letterSpacing: 0.5, marginBottom: 6 },
  summaryValue: { fontSize: 22, fontWeight: '700' },
  summaryCurrency: { fontSize: 12, marginTop: 4 },
  cashFlowCard: { marginBottom: 16 },
  cashFlowRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  cashFlowLabel: { fontSize: 16, fontWeight: '600' },
  cashFlowValue: { fontSize: 18, fontWeight: '700' },
  budgetRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', borderTopWidth: 1, marginTop: 12, paddingTop: 12 },
  statLabel: { fontSize: 14 },
  statValue: { fontSize: 14, fontWeight: '600' },
  card: { marginBottom: 16 },
  cardTitle: { fontSize: 16, fontWeight: '700', marginBottom: 16 },
  catRow: { marginBottom: 14 },
  catInfo: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 6 },
  catName: { fontSize: 14, fontWeight: '500', flex: 1 },
  catAmount: { fontSize: 14, fontWeight: '600', marginLeft: 8 },
  catBar: { height: 6, borderRadius: 3, overflow: 'hidden' },
  catBarFill: { height: '100%', borderRadius: 3 },
  topRow: { flexDirection: 'row', alignItems: 'center', paddingVertical: 10, borderBottomWidth: 1 },
  topRank: { width: 28, height: 28, borderRadius: 8, justifyContent: 'center', alignItems: 'center', marginRight: 12 },
  topRankText: { fontSize: 13, fontWeight: '700' },
  topName: { flex: 1, fontSize: 14, fontWeight: '500' },
  topAmount: { fontSize: 14, fontWeight: '600', marginLeft: 8 },
  emptyWrap: { paddingTop: 60, alignItems: 'center', paddingHorizontal: 24 },
  emptyTitle: { fontSize: 18, fontWeight: '600', marginBottom: 8 },
  emptyBody: { fontSize: 14, textAlign: 'center' },
});
