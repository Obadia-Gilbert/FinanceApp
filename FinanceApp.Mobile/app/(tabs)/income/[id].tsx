import { useState, useEffect } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity, Alert } from 'react-native';
import { useRouter, useLocalSearchParams } from 'expo-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTheme } from '../../../src/context/ThemeContext';
import { Input } from '../../../src/components/Input';
import { Button } from '../../../src/components/Button';
import { getIncome, updateIncome, deleteIncome } from '../../../src/api/income';
import { getCategories } from '../../../src/api/categories';
import { getAccounts } from '../../../src/api/accounts';
import { ApiError } from '../../../src/api/client';
import { CURRENCY_LIST } from '../../../src/utils/currency';

export default function EditIncomeScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const { colors } = useTheme();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState('TZS');
  const [description, setDescription] = useState('');
  const [source, setSource] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [accountId, setAccountId] = useState('');
  const [date, setDate] = useState('');
  const [error, setError] = useState('');

  const { data: income } = useQuery({
    queryKey: ['income', id],
    queryFn: () => getIncome(id!),
    enabled: !!id,
  });
  const { data: categories = [] } = useQuery({ queryKey: ['categories'], queryFn: getCategories });
  const { data: accounts = [] } = useQuery({ queryKey: ['accounts'], queryFn: getAccounts });
  const incomeCategories = categories.filter((c) => c.type === 'Income' || c.type === 'Both');

  useEffect(() => {
    if (income) {
      setAmount(String(income.amount));
      setCurrency(income.currency ?? 'TZS');
      setDescription(income.description ?? '');
      setSource(income.source ?? '');
      setCategoryId(income.categoryId);
      setAccountId(income.accountId ?? '');
      setDate(income.incomeDate ? new Date(income.incomeDate).toISOString().slice(0, 10) : '');
    }
  }, [income]);

  const updateMutation = useMutation({
    mutationFn: (payload: Parameters<typeof updateIncome>[1]) => updateIncome(id!, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['incomes'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      router.back();
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : 'Failed to update'),
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteIncome(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['incomes'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
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
      incomeDate: date,
      categoryId,
      accountId: accountId || null,
      description: description.trim() || null,
      source: source.trim() || null,
    });
  };

  if (!id || !income) {
    return (
      <View style={[styles.centered, { backgroundColor: colors.bg.alt }]}>
        <Text style={{ color: colors.text.muted }}>Loading...</Text>
      </View>
    );
  }

  return (
    <ScrollView style={[styles.container, { backgroundColor: colors.bg.alt }]} contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
      <Input label="Amount" value={amount} onChangeText={setAmount} placeholder="0.00" keyboardType="decimal-pad" />
      <View style={styles.field}>
        <Text style={[styles.label, { color: colors.text.body }]}>Currency</Text>
        <ScrollView horizontal showsHorizontalScrollIndicator={false}>
          {CURRENCY_LIST.map((c) => (
            <TouchableOpacity
              key={c}
              onPress={() => setCurrency(c)}
              style={[styles.chip, { backgroundColor: currency === c ? colors.brand : colors.bg.hover }]}
            >
              <Text style={[styles.chipText, { color: currency === c ? '#fff' : colors.text.body }]}>{c}</Text>
            </TouchableOpacity>
          ))}
        </ScrollView>
      </View>
      <Input label="Date" value={date} onChangeText={setDate} placeholder="YYYY-MM-DD" />
      <Input label="Description" value={description} onChangeText={setDescription} />
      <Input label="Source" value={source} onChangeText={setSource} />
      <View style={styles.field}>
        <Text style={[styles.label, { color: colors.text.body }]}>Category</Text>
        <View style={styles.wrap}>
          {incomeCategories.map((c) => (
            <TouchableOpacity
              key={c.id}
              onPress={() => setCategoryId(c.id)}
              style={[styles.catBtn, { backgroundColor: categoryId === c.id ? colors.bg.hover : colors.bg.default, borderColor: colors.border }]}
            >
              <Text style={[styles.catText, { color: colors.text.primary }]}>{c.name}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>
      {error ? <Text style={[styles.err, { color: colors.danger }]}>{error}</Text> : null}
      <Button title="Save" onPress={handleSave} loading={updateMutation.isPending} style={styles.btn} />
      <Button title="Delete" variant="danger" onPress={() => Alert.alert('Delete', 'Delete this income?', [{ text: 'Cancel', style: 'cancel' }, { text: 'Delete', style: 'destructive', onPress: () => deleteMutation.mutate() }])} loading={deleteMutation.isPending} style={styles.deleteBtn} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 16, paddingBottom: 40 },
  centered: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  field: { marginBottom: 16 },
  label: { fontSize: 14, fontWeight: '500', marginBottom: 8 },
  chip: { paddingHorizontal: 14, paddingVertical: 10, borderRadius: 8, marginRight: 8 },
  chipText: { fontSize: 15 },
  wrap: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  catBtn: { paddingHorizontal: 14, paddingVertical: 10, borderRadius: 8, borderWidth: 1 },
  catText: { fontSize: 14 },
  err: { marginBottom: 12, fontSize: 14 },
  btn: { marginTop: 8 },
  deleteBtn: { marginTop: 12 },
});
