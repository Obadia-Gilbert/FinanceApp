import { useState } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity } from 'react-native';
import { useRouter } from 'expo-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTheme } from '../../../src/context/ThemeContext';
import { Input } from '../../../src/components/Input';
import { Button } from '../../../src/components/Button';
import { getAccounts } from '../../../src/api/accounts';
import { getCategories } from '../../../src/api/categories';
import { createRecurringTemplate } from '../../../src/api/recurring';
import { ApiError } from '../../../src/api/client';
import { CURRENCY_LIST, getCurrencyIndex } from '../../../src/utils/currency';

const FREQUENCIES = [
  { label: 'Weekly', value: 0 },
  { label: 'Monthly', value: 1 },
  { label: 'Yearly', value: 2 },
];

export default function CreateRecurringScreen() {
  const { colors } = useTheme();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [accountId, setAccountId] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [type, setType] = useState<0 | 1>(1); // 0=Income, 1=Expense
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState('TZS');
  const [frequency, setFrequency] = useState(1);
  const [startDate, setStartDate] = useState(new Date().toISOString().slice(0, 10));
  const [endDate, setEndDate] = useState('');
  const [interval, setInterval] = useState('1');
  const [note, setNote] = useState('');
  const [error, setError] = useState('');

  const { data: accounts = [] } = useQuery({ queryKey: ['accounts'], queryFn: getAccounts });
  const { data: categories = [] } = useQuery({ queryKey: ['categories'], queryFn: getCategories });
  const activeAccounts = accounts.filter((a) => a.isActive);
  const expenseCategories = categories.filter((c) => c.type === 'Expense' || c.type === 'Both');
  const incomeCategories = categories.filter((c) => c.type === 'Income' || c.type === 'Both');
  const typeCategories = type === 0 ? incomeCategories : expenseCategories;

  const createMutation = useMutation({
    mutationFn: createRecurringTemplate,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['recurring'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      router.back();
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : 'Failed to create'),
  });

  const handleSubmit = () => {
    setError('');
    if (!accountId) {
      setError('Select an account');
      return;
    }
    const num = parseFloat(amount.replace(/,/g, '.'));
    if (isNaN(num) || num <= 0) {
      setError('Enter a valid amount');
      return;
    }
    const intervalNum = parseInt(interval, 10) || 1;
    const currencyNum = getCurrencyIndex(currency);

    createMutation.mutate({
      accountId,
      categoryId: categoryId || null,
      type,
      amount: num,
      currency: currencyNum,
      frequency,
      startDate: startDate + 'T00:00:00Z',
      endDate: endDate.trim() ? endDate + 'T00:00:00Z' : null,
      interval: intervalNum,
      note: note.trim() || null,
    });
  };

  return (
    <ScrollView
      style={[styles.container, { backgroundColor: colors.bg.alt }]}
      contentContainerStyle={styles.content}
      keyboardShouldPersistTaps="handled"
    >
      <View style={styles.field}>
        <Text style={[styles.label, { color: colors.text.body }]}>Type</Text>
        <View style={styles.row}>
          <TouchableOpacity
            style={[styles.chip, { backgroundColor: type === 0 ? colors.success : colors.bg.hover }]}
            onPress={() => setType(0)}
          >
            <Text style={[styles.chipText, { color: type === 0 ? '#fff' : colors.text.body }]}>Income</Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.chip, { backgroundColor: type === 1 ? colors.danger : colors.bg.hover }]}
            onPress={() => setType(1)}
          >
            <Text style={[styles.chipText, { color: type === 1 ? '#fff' : colors.text.body }]}>Expense</Text>
          </TouchableOpacity>
        </View>
      </View>

      <View style={styles.field}>
        <Text style={[styles.label, { color: colors.text.body }]}>Account *</Text>
        <View style={styles.wrap}>
          {activeAccounts.map((a) => (
            <TouchableOpacity
              key={a.id}
              onPress={() => setAccountId(a.id)}
              style={[
                styles.opt,
                { borderColor: colors.border, backgroundColor: accountId === a.id ? colors.brand : colors.bg.default },
              ]}
            >
              <Text style={[styles.optText, { color: accountId === a.id ? '#fff' : colors.text.primary }]}>{a.name}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>

      <Input label="Amount *" value={amount} onChangeText={setAmount} placeholder="0.00" keyboardType="decimal-pad" />
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
      <View style={styles.field}>
        <Text style={[styles.label, { color: colors.text.body }]}>Frequency</Text>
        <View style={styles.row}>
          {FREQUENCIES.map((f) => (
            <TouchableOpacity
              key={f.value}
              onPress={() => setFrequency(f.value)}
              style={[styles.chip, { backgroundColor: frequency === f.value ? colors.brand : colors.bg.hover }]}
            >
              <Text style={[styles.chipText, { color: frequency === f.value ? '#fff' : colors.text.body }]}>{f.label}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>
      <Input label="Start date *" value={startDate} onChangeText={setStartDate} placeholder="YYYY-MM-DD" />
      <Input label="End date (optional)" value={endDate} onChangeText={setEndDate} placeholder="YYYY-MM-DD" />
      <Input label="Interval (e.g. every 2 months = 2)" value={interval} onChangeText={setInterval} keyboardType="number-pad" />
      <View style={styles.field}>
        <Text style={[styles.label, { color: colors.text.body }]}>Category (optional)</Text>
        <View style={styles.wrap}>
          {typeCategories.map((c) => (
            <TouchableOpacity
              key={c.id}
              onPress={() => setCategoryId(categoryId === c.id ? '' : c.id)}
              style={[
                styles.opt,
                { borderColor: colors.border, backgroundColor: categoryId === c.id ? colors.brand : colors.bg.default },
              ]}
            >
              <Text style={[styles.optText, { color: categoryId === c.id ? '#fff' : colors.text.primary }]}>{c.name}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>
      <Input label="Note (optional)" value={note} onChangeText={setNote} placeholder="e.g. Rent" />
      {error ? <Text style={[styles.err, { color: colors.danger }]}>{error}</Text> : null}
      <Button title="Add recurring" onPress={handleSubmit} loading={createMutation.isPending} style={styles.btn} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 16, paddingBottom: 40 },
  field: { marginBottom: 16 },
  label: { fontSize: 14, fontWeight: '500', marginBottom: 8 },
  row: { flexDirection: 'row', gap: 10 },
  chip: { paddingHorizontal: 14, paddingVertical: 10, borderRadius: 8, marginRight: 8 },
  chipText: { fontSize: 15, fontWeight: '500' },
  wrap: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  opt: { paddingHorizontal: 14, paddingVertical: 10, borderRadius: 8, borderWidth: 1 },
  optText: { fontSize: 14 },
  err: { marginBottom: 12, fontSize: 14 },
  btn: { marginTop: 8 },
});
