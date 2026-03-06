import { useState, useEffect } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity, Alert, ActivityIndicator } from 'react-native';
import { useRouter, useLocalSearchParams } from 'expo-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTheme } from '../../../src/context/ThemeContext';
import { Input } from '../../../src/components/Input';
import { Button } from '../../../src/components/Button';
import { getExpense, updateExpense, deleteExpense } from '../../../src/api/expenses';
import { getCategories } from '../../../src/api/categories';
import { ApiError } from '../../../src/api/client';
import { CURRENCY_LIST, formatCurrencyCode } from '../../../src/utils/currency';

export default function ExpenseDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const { colors } = useTheme();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState('TZS');
  const [description, setDescription] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [date, setDate] = useState('');
  const [error, setError] = useState('');

  const { data: expense, isLoading } = useQuery({
    queryKey: ['expense', id],
    queryFn: () => getExpense(id!),
    enabled: !!id,
  });

  const { data: categories = [] } = useQuery({
    queryKey: ['categories'],
    queryFn: getCategories,
  });

  useEffect(() => {
    if (expense) {
      setAmount(String(expense.amount));
      setCurrency(expense.currency != null ? formatCurrencyCode(expense.currency) : 'TZS');
      setDescription(expense.description ?? '');
      setCategoryId(expense.categoryId);
      setDate(expense.expenseDate ? new Date(expense.expenseDate).toISOString().slice(0, 10) : '');
    }
  }, [expense]);

  const expenseCategories = categories.filter((c) => c.type === 'Expense' || c.type === 'Both');

  const updateMutation = useMutation({
    mutationFn: (payload: { amount: number; currency: string; expenseDate: string; categoryId: string; description: string | null }) =>
      updateExpense(id!, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['expenses'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      queryClient.invalidateQueries({ queryKey: ['budget'] });
      queryClient.invalidateQueries({ queryKey: ['categoryBudgets'] });
      queryClient.invalidateQueries({ queryKey: ['notificationsUnreadCount'] });
      router.back();
    },
    onError: (e) => {
      setError(e instanceof ApiError ? e.message : 'Failed to update');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteExpense(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['expenses'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      queryClient.invalidateQueries({ queryKey: ['budget'] });
      queryClient.invalidateQueries({ queryKey: ['categoryBudgets'] });
      queryClient.invalidateQueries({ queryKey: ['notificationsUnreadCount'] });
      router.back();
    },
  });

  const handleSave = () => {
    setError('');
    const num = parseFloat(amount.replace(/,/g, '.'));
    if (isNaN(num) || num <= 0) {
      setError('Enter a valid amount');
      return;
    }
    if (!categoryId) {
      setError('Select a category');
      return;
    }
    updateMutation.mutate({
      amount: num,
      currency,
      expenseDate: date,
      categoryId,
      description: description.trim() || null,
    });
  };

  const handleDelete = () => {
    Alert.alert('Delete expense', 'Are you sure you want to delete this expense?', [
      { text: 'Cancel', style: 'cancel' },
      { text: 'Delete', style: 'destructive', onPress: () => deleteMutation.mutate() },
    ]);
  };

  if (!id || (expense === undefined && !isLoading)) {
    return (
      <View style={[styles.centered, { backgroundColor: colors.bg.alt }]}>
        <Text style={{ color: colors.text.muted }}>Expense not found</Text>
      </View>
    );
  }

  if (isLoading || !expense) {
    return (
      <View style={[styles.centered, { backgroundColor: colors.bg.alt }]}>
        <ActivityIndicator size="large" color={colors.brand} />
      </View>
    );
  }

  return (
    <ScrollView
      style={[styles.container, { backgroundColor: colors.bg.alt }]}
      contentContainerStyle={styles.content}
      keyboardShouldPersistTaps="handled"
    >
      <Input label="Amount" value={amount} onChangeText={setAmount} placeholder="0.00" keyboardType="decimal-pad" />
      <View style={styles.row}>
        <Text style={[styles.label, { color: colors.text.body }]}>Currency</Text>
        <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.chipScroll}>
          {CURRENCY_LIST.map((c) => (
            <TouchableOpacity
              key={c}
              onPress={() => setCurrency(c)}
              style={[
                styles.chip,
                { borderColor: colors.border, backgroundColor: currency === c ? colors.brand : colors.bg.default },
              ]}
            >
              <Text style={[styles.chipText, { color: currency === c ? '#fff' : colors.text.body }]}>{c}</Text>
            </TouchableOpacity>
          ))}
        </ScrollView>
      </View>
      <Input label="Date" value={date} onChangeText={setDate} placeholder="YYYY-MM-DD" />
      <Input label="Description" value={description} onChangeText={setDescription} placeholder="Description" />
      <View style={styles.field}>
        <Text style={[styles.label, { color: colors.text.body }]}>Category</Text>
        <View style={styles.categoryWrap}>
          {expenseCategories.map((c) => (
            <TouchableOpacity
              key={c.id}
              onPress={() => setCategoryId(c.id)}
              style={[
                styles.catBtn,
                {
                  borderColor: categoryId === c.id ? colors.brand : colors.border,
                  backgroundColor: categoryId === c.id ? `${colors.brand}15` : colors.bg.default,
                  borderWidth: categoryId === c.id ? 2 : 1,
                },
              ]}
            >
              <Text style={[styles.catText, { color: colors.text.primary }]} numberOfLines={1}>
                {c.name}
              </Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>
      {error ? <Text style={[styles.err, { color: colors.danger }]}>{error}</Text> : null}
      <Button title="Save" onPress={handleSave} loading={updateMutation.isPending} style={styles.submit} />
      <Button title="Delete" onPress={handleDelete} variant="danger" loading={deleteMutation.isPending} style={styles.deleteBtn} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 16, paddingBottom: 40 },
  centered: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  label: { fontSize: 14, fontWeight: '500', marginBottom: 8 },
  row: { marginBottom: 16 },
  chipScroll: { marginTop: 4 },
  chip: {
    paddingHorizontal: 14,
    paddingVertical: 10,
    borderRadius: 8,
    marginRight: 8,
  },
  chipText: { fontSize: 15, fontWeight: '500' },
  field: { marginBottom: 16 },
  categoryWrap: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  catBtn: { paddingHorizontal: 14, paddingVertical: 10, borderRadius: 8 },
  catText: { fontSize: 14 },
  err: { marginBottom: 12, fontSize: 14 },
  submit: { marginTop: 8 },
  deleteBtn: { marginTop: 12 },
});
