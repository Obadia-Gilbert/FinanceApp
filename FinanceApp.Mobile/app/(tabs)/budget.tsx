import { useState, useCallback, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  RefreshControl,
  TouchableOpacity,
  Pressable,
  Platform,
  Alert,
} from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import Svg, { Circle } from 'react-native-svg';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTheme } from '../../src/context/ThemeContext';
import { Card } from '../../src/components/Card';
import { Input } from '../../src/components/Input';
import { Button } from '../../src/components/Button';
import { getDashboard } from '../../src/api/dashboard';
import { getBudget, getCategoryBudgets, setBudget, setCategoryBudget, deleteCategoryBudget } from '../../src/api/budget';
import { getCategories } from '../../src/api/categories';
import type { CategoryBudgetDto } from '../../src/types/api';
import { CURRENCY_LIST, getCurrencyIndex, formatCurrencyCode } from '../../src/utils/currency';

const MONTHS = 'January February March April May June July August September October November December'.split(' ');

const SIZE = 140;
const STROKE = 12;
const R = (SIZE - STROKE) / 2;

function CircularProgress({ progress, color, bgColor }: { progress: number; color: string; bgColor: string }) {
  const p = Math.min(1, Math.max(0, progress));
  const circumference = 2 * Math.PI * R;
  const offset = circumference * (1 - p);
  return (
    <Svg width={SIZE} height={SIZE} style={styles.svg}>
      <Circle
        cx={SIZE / 2}
        cy={SIZE / 2}
        r={R}
        stroke={bgColor}
        strokeWidth={STROKE}
        fill="transparent"
      />
      <Circle
        cx={SIZE / 2}
        cy={SIZE / 2}
        r={R}
        stroke={color}
        strokeWidth={STROKE}
        fill="transparent"
        strokeDasharray={circumference}
        strokeDashoffset={offset}
        transform={`rotate(-90 ${SIZE / 2} ${SIZE / 2})`}
        strokeLinecap="round"
      />
    </Svg>
  );
}

const CATEGORY_ICONS: Record<string, string> = {
  food: '🍽️',
  transport: '🚗',
  entertainment: '🎬',
  shopping: '🛒',
  health: '❤️',
  bills: '📄',
  default: '📊',
};

function categoryIcon(name: string | null): string {
  if (!name) return CATEGORY_ICONS.default;
  const key = Object.keys(CATEGORY_ICONS).find((k) => name.toLowerCase().includes(k));
  return key ? CATEGORY_ICONS[key] : CATEGORY_ICONS.default;
}

export default function BudgetScreen() {
  const { colors } = useTheme();
  const queryClient = useQueryClient();
  const now = new Date();
  const [month, setMonth] = useState(now.getMonth() + 1);
  const [year, setYear] = useState(now.getFullYear());
  const [editing, setEditing] = useState(false);
  const [amountStr, setAmountStr] = useState('');
  const [currency, setCurrency] = useState('USD');
  const [showCategoryForm, setShowCategoryForm] = useState(false);
  const [categoryBudgetCategoryId, setCategoryBudgetCategoryId] = useState('');
  const [categoryBudgetAmount, setCategoryBudgetAmount] = useState('');
  const [categoryBudgetCurrency, setCategoryBudgetCurrency] = useState('USD');

  useEffect(() => {
    setEditing(false);
    setShowCategoryForm(false);
    setAmountStr('');
    setCategoryBudgetCategoryId('');
    setCategoryBudgetAmount('');
  }, [month, year]);

  const { data: dashboard } = useQuery({
    queryKey: ['dashboard'],
    queryFn: getDashboard,
  });

  const { data: budget, refetch, isRefetching } = useQuery({
    queryKey: ['budget', month, year],
    queryFn: () => getBudget(month, year),
  });

  const { data: categoryBudgets = [], refetch: refetchCategoryBudgets } = useQuery({
    queryKey: ['categoryBudgets', month, year],
    queryFn: async () => {
      try {
        return await getCategoryBudgets(month, year);
      } catch {
        return [];
      }
    },
  });

  // Refetch budget and category budgets when screen is focused (e.g. after adding an expense)
  useFocusEffect(
    useCallback(() => {
      refetch();
      refetchCategoryBudgets();
    }, [refetch, refetchCategoryBudgets])
  );

  const { data: categories = [] } = useQuery({
    queryKey: ['categories'],
    queryFn: getCategories,
  });
  const budgetEligibleCategories = categories.filter((c) => c.type === 'Expense' || c.type === 'Both');

  const setBudgetMutation = useMutation({
    mutationFn: setBudget,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['budget', month, year] });
      queryClient.invalidateQueries({ queryKey: ['categoryBudgets', month, year] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      setEditing(false);
      setAmountStr('');
    },
  });

  const handleSetBudget = () => {
    const num = parseFloat(amountStr.replace(/,/g, '.'));
    if (isNaN(num) || num < 0) return;
    setBudgetMutation.mutate({
      month,
      year,
      amount: num,
      currency: getCurrencyIndex(currency),
    });
  };

  const setCategoryBudgetMutation = useMutation({
    mutationFn: ({ categoryId, amount }: { categoryId: string; amount: number }) =>
      setCategoryBudget(categoryId, month, year, amount, getCurrencyIndex(categoryBudgetCurrency)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categoryBudgets', month, year] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      setShowCategoryForm(false);
      setCategoryBudgetCategoryId('');
      setCategoryBudgetAmount('');
    },
  });

  const deleteCategoryBudgetMutation = useMutation({
    mutationFn: deleteCategoryBudget,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categoryBudgets', month, year] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });

  const handleSaveCategoryBudget = () => {
    const num = parseFloat(categoryBudgetAmount.replace(/,/g, '.'));
    if (!categoryBudgetCategoryId) return;
    if (isNaN(num) || num < 0) return;
    setCategoryBudgetMutation.mutate({ categoryId: categoryBudgetCategoryId, amount: num });
  };

  const handleDeleteCategoryBudget = (cb: CategoryBudgetDto) => {
    Alert.alert(
      'Remove category budget',
      `Remove budget for ${cb.categoryName ?? 'this category'}?`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Remove',
          style: 'destructive',
          onPress: () => deleteCategoryBudgetMutation.mutate(cb.id),
        },
      ]
    );
  };

  const goPrevMonth = () => {
    if (month <= 1) {
      setMonth(12);
      setYear((y) => y - 1);
    } else {
      setMonth((m) => m - 1);
    }
  };

  const goNextMonth = () => {
    if (month >= 12) {
      setMonth(1);
      setYear((y) => y + 1);
    } else {
      setMonth((m) => m + 1);
    }
  };

  const thisMonthSpend = dashboard?.thisMonthSpend ?? 0;
  const budgetAmount = budget?.amount ?? dashboard?.budgetAmount ?? 0;
  const progress = budgetAmount > 0 ? Math.min(1, thisMonthSpend / budgetAmount) : 0;
  const progressPct = budgetAmount > 0 ? Math.round((thisMonthSpend / budgetAmount) * 100) : 0;
  const isOver = budgetAmount > 0 && thisMonthSpend >= budgetAmount;
  const remaining = budgetAmount > 0 ? Math.max(0, budgetAmount - thisMonthSpend) : 0;
  const displayCurrency = dashboard?.displayCurrency ?? budget?.currency ?? 'USD';

  return (
    <ScrollView
      style={[styles.container, { backgroundColor: colors.bg.alt }]}
      contentContainerStyle={styles.content}
      refreshControl={
        <RefreshControl
          refreshing={isRefetching}
          onRefresh={() => refetch()}
          tintColor={colors.brand}
        />
      }
    >
      <View style={styles.headerBlock}>
        <Text style={[styles.screenTitle, { color: colors.text.primary }]}>Monthly Budget</Text>
        <View
          style={[styles.monthNav, { backgroundColor: colors.bg.default, borderColor: colors.border }]}
          accessibilityLabel={`Budget month ${MONTHS[month - 1]} ${year}`}
        >
          <Pressable
            onPress={goPrevMonth}
            style={({ pressed }) => [styles.monthNavHit, pressed && styles.monthNavHitPressed]}
            hitSlop={12}
            accessibilityRole="button"
            accessibilityLabel="Previous month"
            android_ripple={{ color: Platform.OS === 'android' ? `${colors.brand}33` : undefined, borderless: true }}
          >
            <Text style={[styles.monthNavChevron, { color: colors.brand }]}>‹</Text>
          </Pressable>
          <View style={styles.monthNavLabelCenter}>
            <Text style={[styles.monthNavLabel, { color: colors.text.primary }]}>
              {MONTHS[month - 1]} {year}
            </Text>
          </View>
          <Pressable
            onPress={goNextMonth}
            style={({ pressed }) => [styles.monthNavHit, pressed && styles.monthNavHitPressed]}
            hitSlop={12}
            accessibilityRole="button"
            accessibilityLabel="Next month"
            android_ripple={{ color: Platform.OS === 'android' ? `${colors.brand}33` : undefined, borderless: true }}
          >
            <Text style={[styles.monthNavChevron, { color: colors.brand }]}>›</Text>
          </Pressable>
        </View>
      </View>

      {/* Total Monthly Budget card with circular progress */}
      <Card style={styles.summaryCard}>
        <View style={styles.circleWrap}>
          <CircularProgress
            progress={progress}
            color={isOver ? colors.danger : colors.brand}
            bgColor={colors.border}
          />
          <View style={StyleSheet.absoluteFillObject} pointerEvents="none">
            <View style={styles.circleCenter}>
              <Text style={[styles.circlePct, { color: colors.text.primary }]}>{progressPct}%</Text>
              <Text style={[styles.circleLabel, { color: colors.text.muted }]}>SPENT</Text>
            </View>
          </View>
        </View>
        <Text style={[styles.summaryTitle, { color: colors.text.primary }]}>Total Monthly Budget</Text>
        <Text style={[styles.summaryRemaining, { color: colors.text.body }]}>
          You have {displayCurrency} {remaining.toLocaleString()} remaining for {MONTHS[month - 1]}
        </Text>
        <View style={[styles.summaryRow, { borderTopColor: colors.border }]}>
          <View style={styles.summaryCol}>
            <Text style={[styles.summaryColLabel, { color: colors.text.muted }]}>SPENT</Text>
            <Text style={[styles.summaryColValue, { color: colors.text.primary }]}>
              {displayCurrency} {thisMonthSpend.toLocaleString()}
            </Text>
          </View>
          <View style={[styles.summaryDivider, { backgroundColor: colors.border }]} />
          <View style={styles.summaryCol}>
            <Text style={[styles.summaryColLabel, { color: colors.text.muted }]}>BUDGET</Text>
            <Text style={[styles.summaryColValue, { color: colors.text.primary }]}>
              {displayCurrency} {budgetAmount > 0 ? budgetAmount.toLocaleString() : '—'}
            </Text>
          </View>
        </View>
      </Card>

      {/* Category Budgets */}
      <View style={styles.section}>
        <Text style={[styles.sectionTitle, { color: colors.text.primary }]}>Category budgets</Text>
        <Text style={[styles.sectionSubtitle, { color: colors.text.muted }]}>
          Choose how you want to cap spending for this month.
        </Text>
        <View style={styles.actionTileRow}>
          <Pressable
            onPress={() => {
              setEditing(false);
              setShowCategoryForm(true);
            }}
            style={({ pressed }) => [
              styles.actionTile,
              {
                backgroundColor: colors.bg.default,
                borderColor: showCategoryForm ? colors.brand : colors.border,
                borderWidth: showCategoryForm ? 2 : 1,
                opacity: pressed ? 0.92 : 1,
              },
            ]}
            accessibilityRole="button"
            accessibilityLabel="Add budget by category"
            accessibilityState={{ selected: showCategoryForm }}
            android_ripple={{ color: `${colors.brand}22` }}
          >
            <View style={[styles.actionTileIconWrap, { backgroundColor: colors.brandLight }]}>
              <Text style={[styles.actionTileIcon, { color: colors.brand }]}>⊞</Text>
            </View>
            <Text style={[styles.actionTileTitle, { color: colors.text.primary }]}>By category</Text>
            <Text style={[styles.actionTileHint, { color: colors.text.muted }]}>
              Limit groceries, transport, etc.
            </Text>
          </Pressable>
          <Pressable
            onPress={() => {
              setShowCategoryForm(false);
              setEditing(true);
            }}
            style={({ pressed }) => [
              styles.actionTile,
              {
                backgroundColor: colors.bg.default,
                borderColor: editing ? colors.brand : colors.border,
                borderWidth: editing ? 2 : 1,
                opacity: pressed ? 0.92 : 1,
              },
            ]}
            accessibilityRole="button"
            accessibilityLabel="Set total monthly budget"
            accessibilityState={{ selected: editing }}
            android_ripple={{ color: `${colors.brand}22` }}
          >
            <View style={[styles.actionTileIconWrap, { backgroundColor: colors.brandLight }]}>
              <Text style={[styles.actionTileIcon, { color: colors.brand }]}>∑</Text>
            </View>
            <Text style={[styles.actionTileTitle, { color: colors.text.primary }]}>Total budget</Text>
            <Text style={[styles.actionTileHint, { color: colors.text.muted }]}>
              One cap for the whole month
            </Text>
          </Pressable>
        </View>

        {/* Add category budget form */}
        {showCategoryForm && (
          <Card style={styles.formCard}>
            <Text style={[styles.formTitle, { color: colors.text.primary }]}>Add category budget</Text>
            <Text style={[styles.currencyLabel, { color: colors.text.body, marginBottom: 8 }]}>Category</Text>
            <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.catPickerScroll}>
              {budgetEligibleCategories.map((c) => (
                <TouchableOpacity
                  key={c.id}
                  onPress={() => setCategoryBudgetCategoryId(c.id)}
                  style={[
                    styles.currencyChip,
                    { borderColor: colors.border, backgroundColor: categoryBudgetCategoryId === c.id ? colors.brand : colors.bg.hover },
                  ]}
                >
                  <Text style={[styles.currencyChipText, { color: categoryBudgetCategoryId === c.id ? '#fff' : colors.text.body }]} numberOfLines={1}>
                    {c.name}
                  </Text>
                </TouchableOpacity>
              ))}
            </ScrollView>
            {budgetEligibleCategories.length === 0 && (
              <Text style={[styles.helperText, { color: colors.text.muted }]}>No expense categories. Add categories in More → Categories.</Text>
            )}
            <Input
              label="Amount"
              value={categoryBudgetAmount}
              onChangeText={setCategoryBudgetAmount}
              placeholder="0"
              keyboardType="decimal-pad"
            />
            <View style={styles.currencyRow}>
              <Text style={[styles.currencyLabel, { color: colors.text.body }]}>Currency</Text>
              <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.currencyScroll}>
                {CURRENCY_LIST.map((c) => (
                  <TouchableOpacity
                    key={c}
                    onPress={() => setCategoryBudgetCurrency(c)}
                    style={[styles.currencyChip, { borderColor: colors.border, backgroundColor: categoryBudgetCurrency === c ? colors.brand : colors.bg.hover }]}
                  >
                    <Text style={[styles.currencyChipText, { color: categoryBudgetCurrency === c ? '#fff' : colors.text.body }]}>{c}</Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>
            </View>
            <View style={styles.formBtns}>
              <Button
                title="Cancel"
                onPress={() => { setShowCategoryForm(false); setCategoryBudgetCategoryId(''); setCategoryBudgetAmount(''); }}
                variant="ghost"
                style={styles.formBtn}
              />
              <Button
                title="Save"
                onPress={handleSaveCategoryBudget}
                loading={setCategoryBudgetMutation.isPending}
                disabled={!categoryBudgetCategoryId || !categoryBudgetAmount.trim()}
                style={styles.formBtn}
              />
            </View>
          </Card>
        )}

        {/* Set/Update total budget form - shown right below button so it's visible without scrolling */}
        {editing && (
          <Card style={styles.formCard}>
            <Text style={[styles.formTitle, { color: colors.text.primary }]}>
              {budget ? 'Update' : 'Set'} total monthly budget
            </Text>
            <Input
              label="Amount"
              value={amountStr}
              onChangeText={setAmountStr}
              placeholder="0"
              keyboardType="decimal-pad"
            />
            <View style={styles.currencyRow}>
              <Text style={[styles.currencyLabel, { color: colors.text.body }]}>Currency</Text>
              <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.currencyScroll}>
                {CURRENCY_LIST.map((c) => (
                  <TouchableOpacity
                    key={c}
                    onPress={() => setCurrency(c)}
                    style={[styles.currencyChip, { borderColor: colors.border, backgroundColor: currency === c ? colors.brand : colors.bg.hover }]}
                  >
                    <Text style={[styles.currencyChipText, { color: currency === c ? '#fff' : colors.text.body }]}>{c}</Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>
            </View>
            <View style={styles.formBtns}>
              <Button
                title="Cancel"
                onPress={() => { setEditing(false); setAmountStr(''); }}
                variant="ghost"
                style={styles.formBtn}
              />
              <Button
                title="Save"
                onPress={handleSetBudget}
                loading={setBudgetMutation.isPending}
                style={styles.formBtn}
              />
            </View>
          </Card>
        )}

        {categoryBudgets.length > 0 ? (
          categoryBudgets.map((cb: CategoryBudgetDto) => {
            const catProgress = cb.amount > 0 ? cb.spent / cb.amount : 0;
            const catPct = cb.amount > 0 ? Math.round((cb.spent / cb.amount) * 100) : 0;
            const catOver = cb.amount > 0 && cb.spent >= cb.amount;
            return (
              <Card key={cb.id} style={styles.catCard}>
                <View style={styles.catRow}>
                  <View style={[styles.catIconWrap, { backgroundColor: colors.brandLight ?? colors.bg.alt }]}>
                    <Text style={styles.catIcon}>{categoryIcon(cb.categoryName)}</Text>
                  </View>
                  <View style={styles.catBody}>
                    <Text style={[styles.catName, { color: colors.text.primary }]}>
                      {cb.categoryName ?? 'Uncategorized'}
                    </Text>
                    <Text style={[styles.catMeta, { color: colors.text.muted }]}>
                      {formatCurrencyCode(cb.currency)} {cb.spent.toLocaleString()} of {cb.amount.toLocaleString()} spent
                    </Text>
                    <View style={[styles.catBarBg, { backgroundColor: colors.border }]}>
                      <View
                        style={[
                          styles.catBarFill,
                          {
                            width: `${Math.min(100, catProgress * 100)}%`,
                            backgroundColor: catOver ? colors.danger : colors.brand,
                          },
                        ]}
                      />
                    </View>
                  </View>
                  <View style={styles.catRight}>
                    <Text style={[styles.catPct, { color: colors.text.primary }]}>{catPct}%</Text>
                    <TouchableOpacity
                      onPress={() => handleDeleteCategoryBudget(cb)}
                      style={[styles.deleteCatBtn, { backgroundColor: colors.bg.hover }]}
                      hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                      accessibilityLabel="Remove category budget"
                    >
                      <Text style={[styles.deleteCatBtnText, { color: colors.danger }]}>Remove</Text>
                    </TouchableOpacity>
                  </View>
                </View>
              </Card>
            );
          })
        ) : (
          <Card style={styles.emptyCat}>
            <Text style={[styles.emptyCatText, { color: colors.text.muted }]}>
              No per-category limits yet. Use the tiles above to add a category budget or set your overall monthly cap.
            </Text>
          </Card>
        )}
      </View>

      {dashboard?.isOverBudget && (
        <Card style={[styles.alert, { borderLeftWidth: 4, borderLeftColor: colors.danger }]}>
          <Text style={[styles.alertText, { color: colors.danger }]}>
            You've exceeded your budget this month. Consider reducing spending or updating your budget.
          </Text>
        </Card>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 16, paddingBottom: 40 },
  headerBlock: { marginBottom: 20 },
  screenTitle: { fontSize: 22, fontWeight: '700', marginBottom: 12 },
  monthNav: {
    flexDirection: 'row',
    alignItems: 'center',
    borderRadius: 14,
    borderWidth: 1,
    paddingVertical: 4,
    paddingHorizontal: 4,
  },
  monthNavHit: {
    width: 44,
    height: 44,
    justifyContent: 'center',
    alignItems: 'center',
    borderRadius: 12,
  },
  monthNavHitPressed: { opacity: 0.75 },
  monthNavChevron: { fontSize: 28, fontWeight: '300', marginTop: -2 },
  monthNavLabelCenter: { flex: 1, alignItems: 'center', justifyContent: 'center', paddingVertical: 8 },
  monthNavLabel: { fontSize: 16, fontWeight: '600', letterSpacing: 0.2 },
  summaryCard: { alignItems: 'center', paddingVertical: 24, marginBottom: 24 },
  circleWrap: { position: 'relative', marginBottom: 20 },
  svg: { alignSelf: 'center' },
  circleCenter: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  circlePct: { fontSize: 28, fontWeight: '700' },
  circleLabel: { fontSize: 12, marginTop: 2, letterSpacing: 0.5 },
  summaryTitle: { fontSize: 18, fontWeight: '700', marginBottom: 8 },
  summaryRemaining: { fontSize: 14, marginBottom: 16 },
  summaryRow: { flexDirection: 'row', alignItems: 'center', width: '100%', paddingTop: 16, borderTopWidth: 1 },
  summaryCol: { flex: 1, alignItems: 'center' },
  summaryColLabel: { fontSize: 11, letterSpacing: 0.5, marginBottom: 4 },
  summaryColValue: { fontSize: 16, fontWeight: '700' },
  summaryDivider: { width: 1, height: 32 },
  section: { marginBottom: 24 },
  sectionTitle: { fontSize: 18, fontWeight: '700', marginBottom: 6 },
  sectionSubtitle: { fontSize: 14, lineHeight: 20, marginBottom: 14 },
  actionTileRow: { flexDirection: 'row', gap: 12 },
  actionTile: {
    flex: 1,
    minHeight: 116,
    borderRadius: 16,
    paddingVertical: 14,
    paddingHorizontal: 12,
    justifyContent: 'flex-start',
  },
  actionTileIconWrap: {
    width: 40,
    height: 40,
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 10,
  },
  actionTileIcon: { fontSize: 18, fontWeight: '700' },
  actionTileTitle: { fontSize: 16, fontWeight: '700', marginBottom: 4 },
  actionTileHint: { fontSize: 12, lineHeight: 16 },
  catPickerScroll: { marginBottom: 12, maxHeight: 44 },
  helperText: { fontSize: 12, marginBottom: 8 },
  catCard: { marginBottom: 12 },
  catRow: { flexDirection: 'row', alignItems: 'center' },
  catIconWrap: { width: 44, height: 44, borderRadius: 22, justifyContent: 'center', alignItems: 'center', marginRight: 12 },
  catIcon: { fontSize: 22 },
  catBody: { flex: 1, minWidth: 0 },
  catRight: { alignItems: 'flex-end', marginLeft: 8 },
  catName: { fontSize: 16, fontWeight: '600' },
  catMeta: { fontSize: 13, marginTop: 2 },
  catBarBg: { height: 6, borderRadius: 3, marginTop: 8, overflow: 'hidden' },
  catBarFill: { height: '100%', borderRadius: 3 },
  catPct: { fontSize: 15, fontWeight: '700', marginBottom: 4 },
  deleteCatBtn: { paddingHorizontal: 10, paddingVertical: 6, borderRadius: 8 },
  deleteCatBtnText: { fontSize: 12, fontWeight: '600' },
  emptyCat: { padding: 20 },
  emptyCatText: { fontSize: 14, textAlign: 'center' },
  formCard: { marginTop: 8, marginBottom: 16 },
  formTitle: { fontSize: 16, fontWeight: '600', marginBottom: 12 },
  currencyRow: { marginBottom: 16 },
  currencyLabel: { fontSize: 14, fontWeight: '500', marginBottom: 8 },
  currencyScroll: { marginTop: 4 },
  currencyChip: {
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 8,
    marginRight: 8,
    borderWidth: 1,
  },
  currencyChipText: { fontSize: 14, fontWeight: '500' },
  formBtns: { flexDirection: 'row', gap: 12, marginTop: 8 },
  formBtn: { flex: 1 },
  alert: { marginTop: 8 },
  alertText: { fontSize: 14 },
});
