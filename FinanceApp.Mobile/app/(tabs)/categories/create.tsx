import { useState } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity } from 'react-native';
import { useRouter } from 'expo-router';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useTheme } from '../../../src/context/ThemeContext';
import { Card } from '../../../src/components/Card';
import { Input } from '../../../src/components/Input';
import { Button } from '../../../src/components/Button';
import { createCategory } from '../../../src/api/categories';
import { ApiError } from '../../../src/api/client';

const BADGE_COLORS = ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899'];

export default function CreateCategoryScreen() {
  const { colors } = useTheme();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [badgeColor, setBadgeColor] = useState(BADGE_COLORS[0]);
  const [error, setError] = useState('');

  const mutation = useMutation({
    mutationFn: createCategory,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] });
      router.back();
    },
    onError: (e) => {
      setError(e instanceof ApiError ? e.message : 'Failed to create category');
    },
  });

  const handleSubmit = () => {
    setError('');
    if (!name.trim() || name.trim().length < 2) {
      setError('Name must be at least 2 characters');
      return;
    }
    mutation.mutate({ name: name.trim(), description: description.trim() || null, badgeColor });
  };

  return (
    <ScrollView style={[styles.container, { backgroundColor: colors.bg.alt }]} contentContainerStyle={styles.content}>
      <Card style={styles.card}>
        <Input label="Category name" value={name} onChangeText={setName} placeholder="e.g. Food & Dining" />
        <Input label="Description (optional)" value={description} onChangeText={setDescription} placeholder="Optional" />
        <View style={styles.colorRow}>
          <Text style={[styles.colorLabel, { color: colors.text.body }]}>Color</Text>
          <View style={styles.colorWrap}>
            {BADGE_COLORS.map((c) => (
              <TouchableOpacity
                key={c}
                style={[styles.colorDot, { backgroundColor: c }, badgeColor === c && styles.colorDotSelected]}
                onPress={() => setBadgeColor(c)}
              />
            ))}
          </View>
        </View>
        {error ? <Text style={[styles.err, { color: colors.danger }]}>{error}</Text> : null}
        <Button title="Create Category" onPress={handleSubmit} loading={mutation.isPending} style={styles.btn} />
      </Card>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 16, paddingBottom: 40 },
  card: { marginBottom: 16 },
  colorRow: { marginBottom: 20 },
  colorLabel: { fontSize: 14, fontWeight: '500', marginBottom: 8 },
  colorWrap: { flexDirection: 'row', flexWrap: 'wrap', gap: 12 },
  colorDot: { width: 36, height: 36, borderRadius: 18 },
  colorDotSelected: { borderWidth: 3, borderColor: '#111' },
  err: { fontSize: 14, marginBottom: 12 },
  btn: { marginTop: 8 },
});
