import { useState } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity, Image } from 'react-native';
import { useRouter } from 'expo-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTheme } from '../../../src/context/ThemeContext';
import { Input } from '../../../src/components/Input';
import { Button } from '../../../src/components/Button';
import { getCategories } from '../../../src/api/categories';
import { getAccounts } from '../../../src/api/accounts';
import { createIncome } from '../../../src/api/income';
import { uploadSupportingDocument } from '../../../src/api/supportingDocuments';
import { pickSupportingDocument, type PickedImage } from '../../../src/utils/imagePicker';
import { ApiError } from '../../../src/api/client';
import { CURRENCY_LIST } from '../../../src/utils/currency';

export default function CreateIncomeScreen() {
  const { colors } = useTheme();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState('TZS');
  const [description, setDescription] = useState('');
  const [source, setSource] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [accountId, setAccountId] = useState('');
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [attachedDoc, setAttachedDoc] = useState<PickedImage | null>(null);
  const [error, setError] = useState('');

  const { data: categories = [] } = useQuery({ queryKey: ['categories'], queryFn: getCategories });
  const { data: accounts = [] } = useQuery({ queryKey: ['accounts'], queryFn: getAccounts });
  const incomeCategories = categories.filter((c) => c.type === 'Income' || c.type === 'Both');

  const createMutation = useMutation({
    mutationFn: async (payload: Parameters<typeof createIncome>[0] & { attachedDoc: PickedImage | null }) => {
      const { attachedDoc: _doc, ...incomePayload } = payload;
      return createIncome(incomePayload);
    },
    onSuccess: async (created, variables) => {
      queryClient.invalidateQueries({ queryKey: ['incomes'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      if (variables.attachedDoc) {
        try {
          await uploadSupportingDocument(
            'Income',
            created.id,
            variables.attachedDoc.uri,
            variables.attachedDoc.fileName,
            variables.attachedDoc.mimeType,
            'Supporting document'
          );
        } catch (_e) {
          // Income created; document upload failed (user can add later from edit if needed)
        }
      }
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
    if (!categoryId) {
      setError('Select a category');
      return;
    }
    createMutation.mutate({
      amount: num,
      currency,
      incomeDate: date,
      categoryId,
      accountId: accountId || null,
      description: description.trim() || null,
      source: source.trim() || null,
      attachedDoc,
    });
  };

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
              style={[styles.chip, { backgroundColor: currency === c ? colors.brand : colors.bg.default, borderColor: colors.border, borderWidth: 1 }]}
            >
              <Text style={[styles.chipText, { color: currency === c ? '#fff' : colors.text.body }]}>{c}</Text>
            </TouchableOpacity>
          ))}
        </ScrollView>
      </View>
      <Input label="Date" value={date} onChangeText={setDate} placeholder="YYYY-MM-DD" />
      <Input label="Description" value={description} onChangeText={setDescription} placeholder="Optional" />
      <Input label="Source" value={source} onChangeText={setSource} placeholder="e.g. Salary" />
      <View style={styles.field}>
        <Text style={[styles.label, { color: colors.text.body }]}>Supporting document</Text>
        {attachedDoc ? (
          <View style={[styles.docPreview, { backgroundColor: colors.bg.default, borderColor: colors.border }]}>
            <Image source={{ uri: attachedDoc.uri }} style={styles.docThumb} />
            <Text style={[styles.docName, { color: colors.text.body }]} numberOfLines={1}>
              {attachedDoc.fileName}
            </Text>
            <TouchableOpacity
              onPress={() => setAttachedDoc(null)}
              style={[styles.docRemove, { backgroundColor: colors.danger }]}
            >
              <Text style={styles.docRemoveText}>Remove</Text>
            </TouchableOpacity>
          </View>
        ) : (
          <TouchableOpacity
            onPress={async () => {
              const picked = await pickSupportingDocument();
              if (picked) setAttachedDoc(picked);
            }}
            style={[styles.attachBtn, { borderColor: colors.border, backgroundColor: colors.bg.default }]}
          >
            <Text style={styles.attachIcon}>📷</Text>
            <Text style={[styles.attachLabel, { color: colors.text.body }]}>
              Take photo or choose from library
            </Text>
          </TouchableOpacity>
        )}
      </View>
      <View style={styles.field}>
        <Text style={[styles.label, { color: colors.text.body }]}>Category</Text>
        <View style={styles.wrap}>
          {incomeCategories.map((c) => (
            <TouchableOpacity
              key={c.id}
              onPress={() => setCategoryId(c.id)}
              style={[styles.catBtn, { backgroundColor: categoryId === c.id ? `${colors.brand}15` : colors.bg.default, borderColor: categoryId === c.id ? colors.brand : colors.border, borderWidth: categoryId === c.id ? 2 : 1 }]}
            >
              <Text style={[styles.catText, { color: colors.text.primary }]}>{c.name}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>
      <View style={styles.field}>
        <Text style={[styles.label, { color: colors.text.body }]}>Account (optional)</Text>
        <View style={styles.wrap}>
          <TouchableOpacity
            onPress={() => setAccountId('')}
            style={[styles.catBtn, { backgroundColor: !accountId ? `${colors.brand}15` : colors.bg.default, borderColor: !accountId ? colors.brand : colors.border, borderWidth: !accountId ? 2 : 1 }]}
          >
            <Text style={[styles.catText, { color: colors.text.primary }]}>None</Text>
          </TouchableOpacity>
          {accounts.filter((a) => a.isActive).map((a) => (
            <TouchableOpacity
              key={a.id}
              onPress={() => setAccountId(a.id)}
              style={[styles.catBtn, { backgroundColor: accountId === a.id ? `${colors.brand}15` : colors.bg.default, borderColor: accountId === a.id ? colors.brand : colors.border, borderWidth: accountId === a.id ? 2 : 1 }]}
            >
              <Text style={[styles.catText, { color: colors.text.primary }]}>{a.name}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>
      {error ? <Text style={[styles.err, { color: colors.danger }]}>{error}</Text> : null}
      <Button title="Add income" onPress={handleSubmit} loading={createMutation.isPending} style={styles.btn} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 16, paddingBottom: 40 },
  field: { marginBottom: 16 },
  label: { fontSize: 14, fontWeight: '500', marginBottom: 8 },
  chip: { paddingHorizontal: 14, paddingVertical: 10, borderRadius: 8, marginRight: 8 },
  chipText: { fontSize: 15 },
  wrap: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  catBtn: { paddingHorizontal: 14, paddingVertical: 10, borderRadius: 8, borderWidth: 1 },
  catText: { fontSize: 14 },
  err: { marginBottom: 12, fontSize: 14 },
  btn: { marginTop: 8 },
  attachBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 14,
    borderRadius: 8,
    borderWidth: 1,
    gap: 10,
  },
  attachIcon: { fontSize: 22 },
  attachLabel: { fontSize: 15, flex: 1 },
  docPreview: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 10,
    borderRadius: 8,
    borderWidth: 1,
    gap: 10,
  },
  docThumb: { width: 48, height: 48, borderRadius: 6 },
  docName: { flex: 1, fontSize: 14 },
  docRemove: { paddingHorizontal: 12, paddingVertical: 6, borderRadius: 6 },
  docRemoveText: { color: '#fff', fontSize: 14, fontWeight: '500' },
});
