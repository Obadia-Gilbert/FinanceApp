import { useState } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity } from 'react-native';
import { useRouter } from 'expo-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTheme } from '../../../src/context/ThemeContext';
import { Input } from '../../../src/components/Input';
import { Button } from '../../../src/components/Button';
import { getAccounts } from '../../../src/api/accounts';
import { createTransfer } from '../../../src/api/transactions';
import { ApiError } from '../../../src/api/client';
import { CURRENCY_LIST } from '../../../src/utils/currency';

export default function TransferScreen() {
  const { colors } = useTheme();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState('TZS');
  const [fromAccountId, setFromAccountId] = useState('');
  const [toAccountId, setToAccountId] = useState('');
  const [note, setNote] = useState('');
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [error, setError] = useState('');

  const { data: accounts = [] } = useQuery({ queryKey: ['accounts'], queryFn: getAccounts });
  const active = accounts.filter((a) => a.isActive);

  const createMutation = useMutation({
    mutationFn: createTransfer,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['transactions'] });
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
      router.back();
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : 'Failed to transfer'),
  });

  const handleSubmit = () => {
    setError('');
    const num = parseFloat(amount.replace(/,/g, '.'));
    if (isNaN(num) || num <= 0) {
      setError('Enter a valid amount');
      return;
    }
    if (!fromAccountId || !toAccountId) {
      setError('Select from and to accounts');
      return;
    }
    if (fromAccountId === toAccountId) {
      setError('From and to accounts must be different');
      return;
    }
    createMutation.mutate({
      fromAccountId,
      toAccountId,
      amount: num,
      currency,
      date,
      note: note.trim() || null,
    });
  };

  return (
    <ScrollView style={[styles.container, { backgroundColor: colors.bg.alt }]} contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
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
        <Text style={[styles.label, { color: colors.text.body }]}>From account</Text>
        <View style={styles.wrap}>
          {active.map((a) => (
            <TouchableOpacity key={a.id} onPress={() => setFromAccountId(a.id)} style={[styles.catBtn, { backgroundColor: fromAccountId === a.id ? colors.bg.hover : colors.bg.default, borderColor: colors.border }]}>
              <Text style={[styles.catText, { color: colors.text.primary }]}>{a.name}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>
      <View style={styles.field}>
        <Text style={[styles.label, { color: colors.text.body }]}>To account</Text>
        <View style={styles.wrap}>
          {active.map((a) => (
            <TouchableOpacity key={a.id} onPress={() => setToAccountId(a.id)} style={[styles.catBtn, { backgroundColor: toAccountId === a.id ? colors.bg.hover : colors.bg.default, borderColor: colors.border }]}>
              <Text style={[styles.catText, { color: colors.text.primary }]}>{a.name}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>
      <Input label="Note" value={note} onChangeText={setNote} placeholder="Optional" />
      {error ? <Text style={[styles.err, { color: colors.danger }]}>{error}</Text> : null}
      <Button title="Transfer" onPress={handleSubmit} loading={createMutation.isPending} style={styles.btn} />
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
