import { useState } from 'react';
import { ScrollView, Text, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useTheme } from '../../../src/context/ThemeContext';
import { Input } from '../../../src/components/Input';
import { Button } from '../../../src/components/Button';
import { createAccount } from '../../../src/api/accounts';
import { ApiError } from '../../../src/api/client';

const ACCOUNT_TYPES = ['Checking', 'Savings', 'CreditCard', 'Cash', 'Investment'];
const CURRENCIES = ['TZS', 'USD', 'EUR', 'GBP', 'KES'];

export default function CreateAccountScreen() {
  const { colors } = useTheme();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [name, setName] = useState('');
  const [type, setType] = useState('Checking');
  const [currency, setCurrency] = useState('TZS');
  const [initialBalance, setInitialBalance] = useState('0');
  const [description, setDescription] = useState('');
  const [error, setError] = useState('');

  const createMutation = useMutation({
    mutationFn: createAccount,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
      router.back();
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : 'Failed to create'),
  });

  const handleSubmit = () => {
    setError('');
    if (!name.trim()) {
      setError('Name is required');
      return;
    }
    const balance = parseFloat(initialBalance.replace(/,/g, '.'));
    if (isNaN(balance)) {
      setError('Enter a valid balance');
      return;
    }
    createMutation.mutate({
      name: name.trim(),
      type,
      currency,
      initialBalance: balance,
      description: description.trim() || null,
    });
  };

  return (
    <ScrollView style={[styles.container, { backgroundColor: colors.bg.alt }]} contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
      <Input label="Account name" value={name} onChangeText={setName} placeholder="e.g. Main bank" />
      <Input label="Initial balance" value={initialBalance} onChangeText={setInitialBalance} placeholder="0" keyboardType="decimal-pad" />
      <Input label="Currency" value={currency} onChangeText={setCurrency} placeholder="TZS" />
      <Input label="Description (optional)" value={description} onChangeText={setDescription} placeholder="Optional" />
      {error ? <Text style={[styles.err, { color: colors.danger }]}>{error}</Text> : null}
      <Button title="Add account" onPress={handleSubmit} loading={createMutation.isPending} style={styles.btn} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 16, paddingBottom: 40 },
  err: { marginBottom: 12, fontSize: 14 },
  btn: { marginTop: 8 },
});
