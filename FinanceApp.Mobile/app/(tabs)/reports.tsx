import { useState } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity, RefreshControl } from 'react-native';
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

  const { data: report, refetch, isRefetching } = useQuery({
    queryKey: ['report', year, month],
    queryFn: () => getMonthlyReport(year, month),
  });

  return (
    <ScrollView
      style={[styles.container, { backgroundColor: colors.bg.alt }]}
      contentContainerStyle={styles.content}
      refreshControl={<RefreshControl refreshing={isRefetching} onRefresh={() => refetch()} tintColor={colors.brand} />}
    >
      <Text style={[styles.title, { color: colors.text.primary }]}>Monthly report</Text>
      <View style={styles.pickerRow}>
        <View style={styles.pickerWrap}>
          <Text style={[styles.label, { color: colors.text.muted }]}>Month</Text>
          <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.monthScroll}>
            {MONTHS.map((m, i) => (
              <TouchableOpacity
                key={m}
                onPress={() => setMonth(i + 1)}
                style={[styles.monthChip, { backgroundColor: month === i + 1 ? colors.brand : colors.bg.hover }]}
              >
                <Text style={[styles.monthChipText, { color: month === i + 1 ? '#fff' : colors.text.body }]}>{m}</Text>
              </TouchableOpacity>
            ))}
          </ScrollView>
        </View>
        <View style={styles.yearWrap}>
          <Text style={[styles.label, { color: colors.text.muted }]}>Year</Text>
          <TouchableOpacity onPress={() => setYear((y) => y - 1)} style={[styles.arrowBtn, { borderColor: colors.border }]}>
            <Text style={{ color: colors.text.body }}>−</Text>
          </TouchableOpacity>
          <Text style={[styles.yearText, { color: colors.text.primary }]}>{year}</Text>
          <TouchableOpacity onPress={() => setYear((y) => y + 1)} style={[styles.arrowBtn, { borderColor: colors.border }]}>
            <Text style={{ color: colors.text.body }}>+</Text>
          </TouchableOpacity>
        </View>
      </View>

      {report ? (
        <>
          <Card style={styles.card}>
            <Text style={[styles.cardTitle, { color: colors.text.muted }]}>{report.monthName}</Text>
            <View style={styles.statRow}>
              <Text style={[styles.statLabel, { color: colors.text.muted }]}>Total spent</Text>
              <Text style={[styles.statValue, { color: colors.text.primary }]}>
                {report.totalSpent.toLocaleString()} {report.currency}
              </Text>
            </View>
            <View style={styles.statRow}>
              <Text style={[styles.statLabel, { color: colors.text.muted }]}>Total income</Text>
              <Text style={[styles.statValue, { color: colors.success }]}>
                {report.totalIncome.toLocaleString()} {report.currency}
              </Text>
            </View>
            <View style={styles.statRow}>
              <Text style={[styles.statLabel, { color: colors.text.muted }]}>Net cash flow</Text>
              <Text style={[styles.statValue, { color: report.netCashFlow >= 0 ? colors.success : colors.danger }]}>
                {report.netCashFlow.toLocaleString()} {report.currency}
              </Text>
            </View>
            {report.globalBudgetAmount != null && (
              <View style={styles.statRow}>
                <Text style={[styles.statLabel, { color: colors.text.muted }]}>Budget</Text>
                <Text style={[styles.statValue, { color: colors.text.primary }]}>
                  {report.globalBudgetSpent?.toLocaleString() ?? 0} / {report.globalBudgetAmount.toLocaleString()} {report.currency}
                </Text>
              </View>
            )}
          </Card>

          {report.categoryLines && report.categoryLines.length > 0 && (
            <Card style={styles.card}>
              <Text style={[styles.cardTitle, { color: colors.text.primary }]}>By category</Text>
              {report.categoryLines.slice(0, 10).map((line, i) => (
                <View key={i} style={[styles.statRow, { marginTop: 8 }]}>
                  <Text style={[styles.statLabel, { color: colors.text.body }]} numberOfLines={1}>
                    {line.categoryName}
                  </Text>
                  <Text style={[styles.statValue, { color: line.isOverBudget ? colors.danger : colors.text.primary }]}>
                    {line.spent.toLocaleString()} {report.currency}
                  </Text>
                </View>
              ))}
            </Card>
          )}

          {report.topExpenses && report.topExpenses.length > 0 && (
            <Card style={styles.card}>
              <Text style={[styles.cardTitle, { color: colors.text.primary }]}>Top expenses</Text>
              {report.topExpenses.slice(0, 5).map((exp, i) => (
                <View key={i} style={[styles.statRow, { marginTop: 8 }]}>
                  <Text style={[styles.statLabel, { color: colors.text.body }]} numberOfLines={1}>
                    {exp.description || exp.categoryName}
                  </Text>
                  <Text style={[styles.statValue, { color: colors.text.primary }]}>
                    {exp.amount.toLocaleString()} {exp.currency}
                  </Text>
                </View>
              ))}
            </Card>
          )}
        </>
      ) : (
        <Card style={styles.empty}>
          <Text style={[styles.emptyText, { color: colors.text.muted }]}>No data for this month</Text>
        </Card>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 16, paddingBottom: 40 },
  title: { fontSize: 22, fontWeight: '700', marginBottom: 20 },
  pickerRow: { marginBottom: 20 },
  pickerWrap: { marginBottom: 12 },
  label: { fontSize: 12, marginBottom: 6 },
  monthScroll: { marginBottom: 4 },
  monthChip: { paddingHorizontal: 14, paddingVertical: 10, borderRadius: 8, marginRight: 8 },
  monthChipText: { fontSize: 14 },
  yearWrap: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  arrowBtn: { width: 40, height: 40, borderRadius: 8, borderWidth: 1, justifyContent: 'center', alignItems: 'center' },
  yearText: { fontSize: 18, fontWeight: '600', minWidth: 48, textAlign: 'center' },
  card: { marginBottom: 16 },
  cardTitle: { fontSize: 14, fontWeight: '600', marginBottom: 12 },
  statRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  statLabel: { flex: 1, fontSize: 14 },
  statValue: { fontSize: 14, fontWeight: '600' },
  empty: { marginTop: 8 },
  emptyText: { fontSize: 14 },
});
