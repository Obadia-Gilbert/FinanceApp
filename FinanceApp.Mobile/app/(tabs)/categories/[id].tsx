import { useState, useEffect } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity } from 'react-native';
import { useRouter, useLocalSearchParams } from 'expo-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTheme } from '../../../src/context/ThemeContext';
import { Card } from '../../../src/components/Card';
import { Input } from '../../../src/components/Input';
import { Button } from '../../../src/components/Button';
import { getCategory, updateCategory } from '../../../src/api/categories';
import { ApiError } from '../../../src/api/client';

const BADGE_COLORS = ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899'];

export default function EditCategoryScreen() {
  const { colors } = useTheme();
  const router = useRouter();
  const { id } = useLocalSearchParams<{ id: string }>();
  const queryClient = useQueryClient();
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [badgeColor, setBadgeColor] = useState(BADGE_COLORS[0]);
  const [error, setError] = useState('');

  const { data: category, isLoading } = useQuery({
    queryKey: ['category', id],
    queryFn: () => getCategory(id!),
    enabled: !!id,
  });

  useEffect(() => {
    if (category) {
      setName(category.name);
      setDescription(category.description ?? '');
      setBadgeColor(category.badgeColor || BADGE_COLORS[0]);
    }
  }, [category]);

  const mutation = useMutation({
    mutationFn: (body: { name: string; description: string | null; badgeColor: string }) =>
      updateCategory(id!, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] });
      queryClient.invalidateQueries({ queryKey: ['category', id] });
      router.back();
    },
    onError: (e) => {
      setError(e instanceof ApiError ? e.message : 'Failed to update category');
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

  if (!id || (category === undefined && !isLoading)) {
    return (
      <View style={[styles.container, { backgroundColor: colors.bg.alt }]}>
        <Text style={[styles.loading, { color: colors.text.muted }]}>Loading...</Text>
      </View>
    );
  }

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
        <Button title="Save" onPress={handleSubmit} loading={mutation.isPending} style={styles.btn} />
      </Card>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 16, paddingBottom: 40 },
  loading: { padding: 24, textAlign: 'center' },
  card: { marginBottom: 16 },
  colorRow: { marginBottom: 20 },
  colorLabel: { fontSize: 14, fontWeight: '500', marginBottom: 8 },
  colorWrap: { flexDirection: 'row', flexWrap: 'wrap', gap: 12 },
  colorDot: { width: 36, height: 36, borderRadius: 18 },
  colorDotSelected: { borderWidth: 3, borderColor: '#111' },
  err: { fontSize: 14, marginBottom: 12 },
  btn: { marginTop: 8 },
});
