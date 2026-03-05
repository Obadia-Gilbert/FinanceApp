import { useState, useEffect } from 'react';
import { View, Text, StyleSheet, ScrollView, Alert } from 'react-native';
import { useRouter, useLocalSearchParams } from 'expo-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTheme } from '../../../src/context/ThemeContext';
import { Input } from '../../../src/components/Input';
import { Button } from '../../../src/components/Button';
import { getAccount, updateAccount, deactivateAccount } from '../../../src/api/accounts';
import { ApiError } from '../../../src/api/client';

export default function EditAccountScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const { colors } = useTheme();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [error, setError] = useState('');

  const { data: account } = useQuery({
    queryKey: ['account', id],
    queryFn: () => getAccount(id!),
    enabled: !!id,
  });

  useEffect(() => {
    if (account) {
      setName(account.name);
      setDescription(account.description ?? '');
    }
  }, [account]);

  const updateMutation = useMutation({
    mutationFn: (payload: { name: string; description: string | null }) => updateAccount(id!, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
      router.back();
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : 'Failed to update'),
  });

  const deleteMutation = useMutation({
    mutationFn: () => deactivateAccount(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
      router.back();
    },
  });

  const handleSave = () => {
    setError('');
    if (!name.trim()) {
      setError('Name is required');
      return;
    }
    updateMutation.mutate({ name: name.trim(), description: description.trim() || null });
  };

  if (!id || !account) {
    return (
      <View style={[styles.centered, { backgroundColor: colors.bg.alt }]}>
        <Text style={{ color: colors.text.muted }}>Loading...</Text>
      </View>
    );
  }

  return (
    <ScrollView style={[styles.container, { backgroundColor: colors.bg.alt }]} contentContainerStyle={styles.content}>
      <Input label="Account name" value={name} onChangeText={setName} placeholder="Name" />
      <Input label="Description" value={description} onChangeText={setDescription} placeholder="Optional" />
      <Text style={[styles.balance, { color: colors.text.muted }]}>
        Current balance: {Number(account.currentBalance).toLocaleString()} {account.currency}
      </Text>
      {error ? <Text style={[styles.err, { color: colors.danger }]}>{error}</Text> : null}
      <Button title="Save" onPress={handleSave} loading={updateMutation.isPending} style={styles.btn} />
      <Button
        title="Deactivate account"
        variant="danger"
        onPress={() =>
          Alert.alert('Deactivate', 'Deactivate this account? You can add it again later.', [
            { text: 'Cancel', style: 'cancel' },
            { text: 'Deactivate', style: 'destructive', onPress: () => deleteMutation.mutate() },
          ])
        }
        loading={deleteMutation.isPending}
        style={styles.deleteBtn}
      />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 16, paddingBottom: 40 },
  centered: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  balance: { fontSize: 14, marginBottom: 16 },
  err: { marginBottom: 12, fontSize: 14 },
  btn: { marginTop: 8 },
  deleteBtn: { marginTop: 12 },
});
