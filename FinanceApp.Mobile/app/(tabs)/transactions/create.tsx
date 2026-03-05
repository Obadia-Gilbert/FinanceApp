import { useState } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity } from 'react-native';
import { useRouter } from 'expo-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTheme } from '../../../src/context/ThemeContext';
import { Input } from '../../../src/components/Input';
import { Button } from '../../../src/components/Button';
import { getAccounts } from '../../../src/api/accounts';
import { getCategories } from '../../../src/api/categories';
import { createTransaction } from '../../../src/api/transactions';
import { ApiError } from '../../../src/api/client';
import { CURRENCY_LIST } from '../../../src/utils/currency';

const TYPES = ['Income', 'Expense'];

export default function CreateTransactionScreen() {
  const { colors } = useTheme();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState('TZS');
  const [type, setType] = useState<'Income' | 'Expense'>('Expense');
  const [accountId, setAccountId] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [note, setNote] = useState('');
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [error, setError] = useState('');

  const { data: accounts = [] } = useQuery({ queryKey: ['accounts'], queryFn: getAccounts });
  const { data: categories = [] } = useQuery({ queryKey: ['categories'], queryFn: getCategories });
  const activeAccounts = accounts.filter((a) => a.isActive);
  const expenseCategories = categories.filter((c) => c.type === 'Expense' || c.type === 'Both');
  const incomeCategories = categories.filter((c) => c.type === 'Income' || c.type === 'Both');
  const catList = type === 'Income' ? incomeCategories : expenseCategories;

  const createMutation = useMutation({
    mutationFn: createTransaction,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['transactions'] });
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      router.back();
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : 'Failed to create'),
  });

  const handleSubmit = () => {
    setError('');
    const num = parseFloat(amount.replace(/,/g, '.'));
    if (isNaN(num) || num <= 0) {
      setError('Enter a valid amount');
      return;
    }
    if (!accountId) {
      setError('Select an account');
      return;
    }
    createMutation.mutate({
      accountId,
      type,
      amount: num,
      currency,
      date,
      categoryId: categoryId || null,
      note: note.trim() || null,
    });
  };

  return (
    <ScrollView style={[styles.container, { backgroundColor: colors.bg.alt }]} contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
      <View style={styles.field}>
        <Text style={[styles.label, { color: colors.text.body }]}>Type</Text>
        <View style={styles.row}>
          {TYPES.map((t) => (
            <TouchableOpacity key={t} onPress={() => setType(t as 'Income' | 'Expense')} style={[styles.chip, { backgroundColor: type === t ? colors.brand : colors.bg.hover }]}>
              <Text style={[styles.chipText, { color: type === t ? '#fff' : colors.text.body }]}>{t}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>
      <Input label="Amount" value={amount} onChangeText={setAmount} placeholder="0.00" keyboardType="decimal-pad" />
      <View style={styles.field}>
        <Text style={[styles.label, { color: colors.text.body }]}>Currency</Text>
        <View style={styles.row}>
          {CURRENCY_LIST.map((c) => (
            <TouchableOpacity key={c} onPress={() => setCurrency(c)} style={[styles.chip, { backgroundColor: currency === c ? colors.brand : colors.bg.hover }]}>
              <Text style={[styles.chipText, { color: currency === c ? '#fff' : colors.text.body }]}>{c}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>
      <Input label="Date" value={date} onChangeText={setDate} placeholder="YYYY-MM-DD" />
      <View style={styles.field}>
        <Text style={[styles.label, { color: colors.text.body }]}>Account</Text>
        <View style={styles.wrap}>
          {activeAccounts.map((a) => (
            <TouchableOpacity key={a.id} onPress={() => setAccountId(a.id)} style={[styles.catBtn, { backgroundColor: accountId === a.id ? colors.bg.hover : colors.bg.default, borderColor: colors.border }]}>
              <Text style={[styles.catText, { color: colors.text.primary }]}>{a.name}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>
      <View style={styles.field}>
        <Text style={[styles.label, { color: colors.text.body }]}>Category (optional)</Text>
        <View style={styles.wrap}>
          {catList.map((c) => (
            <TouchableOpacity key={c.id} onPress={() => setCategoryId(categoryId === c.id ? '' : c.id)} style={[styles.catBtn, { backgroundColor: categoryId === c.id ? colors.bg.hover : colors.bg.default, borderColor: colors.border }]}>
              <Text style={[styles.catText, { color: colors.text.primary }]}>{c.name}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>
      <Input label="Note" value={note} onChangeText={setNote} placeholder="Optional" />
      {error ? <Text style={[styles.err, { color: colors.danger }]}>{error}</Text> : null}
      <Button title="Add transaction" onPress={handleSubmit} loading={createMutation.isPending} style={styles.btn} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 16, paddingBottom: 40 },
  field: { marginBottom: 16 },
  label: { fontSize: 14, fontWeight: '500', marginBottom: 8 },
  row: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  chip: { paddingHorizontal: 14, paddingVertical: 10, borderRadius: 8 },
  chipText: { fontSize: 15 },
  wrap: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  catBtn: { paddingHorizontal: 14, paddingVertical: 10, borderRadius: 8, borderWidth: 1 },
  catText: { fontSize: 14 },
  err: { marginBottom: 12, fontSize: 14 },
  btn: { marginTop: 8 },
});
