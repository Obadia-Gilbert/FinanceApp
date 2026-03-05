import { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  TextInput,
  RefreshControl,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTheme } from '../../src/context/ThemeContext';
import { Card } from '../../src/components/Card';
import { Button } from '../../src/components/Button';
import { Input } from '../../src/components/Input';
import { getMyFeedback, createFeedback } from '../../src/api/feedback';
import { ApiError } from '../../src/api/client';

const FEEDBACK_TYPES = [
  { label: 'Question', value: 0 },
  { label: 'Suggestion', value: 1 },
  { label: 'Comment', value: 2 },
];

export default function FeedbackScreen() {
  const { colors } = useTheme();
  const insets = useSafeAreaInsets();
  const queryClient = useQueryClient();
  const [type, setType] = useState(0);
  const [subject, setSubject] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  const { data, refetch, isRefetching } = useQuery({
    queryKey: ['feedback'],
    queryFn: () => getMyFeedback(1, 30),
  });

  const createMutation = useMutation({
    mutationFn: createFeedback,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['feedback'] });
      setMessage('');
      setSubject('');
      setSuccess(true);
      setTimeout(() => setSuccess(false), 3000);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : 'Failed to send'),
  });

  const list = data?.items ?? [];

  const handleSubmit = () => {
    setError('');
    if (!message.trim()) {
      setError('Please enter your message.');
      return;
    }
    createMutation.mutate({ type, message: message.trim(), subject: subject.trim() || null });
  };

  return (
    <ScrollView
      style={[styles.container, { backgroundColor: colors.bg.alt }]}
      contentContainerStyle={[styles.content, { paddingTop: insets.top + 16, paddingBottom: insets.bottom + 24 }]}
      keyboardShouldPersistTaps="handled"
      refreshControl={
        <RefreshControl refreshing={isRefetching} onRefresh={() => refetch()} tintColor={colors.brand} />
      }
    >
      <Text style={[styles.sectionTitle, { color: colors.text.primary }]}>Send feedback</Text>
      <Text style={[styles.hint, { color: colors.text.muted }]}>
        Questions, suggestions, or comments — we read them all.
      </Text>
      <View style={styles.field}>
        <Text style={[styles.label, { color: colors.text.body }]}>Type</Text>
        <View style={styles.typeRow}>
          {FEEDBACK_TYPES.map((t) => (
            <TouchableOpacity
              key={t.value}
              onPress={() => setType(t.value)}
              style={[styles.typeChip, { backgroundColor: type === t.value ? colors.brand : colors.bg.default, borderColor: colors.border }]}
            >
              <Text style={[styles.typeChipText, { color: type === t.value ? '#fff' : colors.text.primary }]}>{t.label}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>
      <Input label="Subject (optional)" value={subject} onChangeText={setSubject} placeholder="Short title" />
      <View style={styles.field}>
        <Text style={[styles.label, { color: colors.text.body }]}>Message *</Text>
        <TextInput
          style={[styles.textArea, { color: colors.text.primary, borderColor: colors.border }]}
          placeholder="Your feedback..."
          placeholderTextColor={colors.text.subtle}
          value={message}
          onChangeText={setMessage}
          multiline
          numberOfLines={4}
        />
      </View>
      {error ? <Text style={[styles.err, { color: colors.danger }]}>{error}</Text> : null}
      {success ? <Text style={[styles.success, { color: colors.success }]}>Thanks! Your feedback was sent.</Text> : null}
      <Button title="Send feedback" onPress={handleSubmit} loading={createMutation.isPending} style={styles.submitBtn} />

      <Text style={[styles.historyTitle, { color: colors.text.primary }]}>Your recent feedback</Text>
      {list.length === 0 ? (
        <Card style={styles.empty}>
          <Text style={[styles.emptyText, { color: colors.text.muted }]}>No feedback sent yet.</Text>
        </Card>
      ) : (
        list.map((item) => (
          <Card key={item.id} style={[styles.feedbackCard, { borderColor: colors.border }]}>
            <View style={styles.feedbackHeader}>
              <Text style={[styles.feedbackType, { color: colors.brand }]}>
                {FEEDBACK_TYPES.find((t) => t.value === item.type)?.label ?? 'Feedback'}
              </Text>
              <Text style={[styles.feedbackDate, { color: colors.text.muted }]}>
                {item.createdAt ? new Date(item.createdAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }) : ''}
              </Text>
            </View>
            {item.subject ? <Text style={[styles.feedbackSubject, { color: colors.text.primary }]}>{item.subject}</Text> : null}
            <Text style={[styles.feedbackMessage, { color: colors.text.body }]} numberOfLines={3}>{item.message}</Text>
          </Card>
        ))
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 16 },
  sectionTitle: { fontSize: 18, fontWeight: '700', marginBottom: 4 },
  hint: { fontSize: 14, marginBottom: 16 },
  field: { marginBottom: 16 },
  label: { fontSize: 14, fontWeight: '500', marginBottom: 8 },
  typeRow: { flexDirection: 'row', gap: 10 },
  typeChip: { paddingHorizontal: 14, paddingVertical: 10, borderRadius: 8, borderWidth: 1 },
  typeChipText: { fontSize: 14, fontWeight: '600' },
  textArea: { borderWidth: 1, borderRadius: 10, padding: 12, fontSize: 15, minHeight: 100, textAlignVertical: 'top' },
  err: { marginBottom: 8, fontSize: 14 },
  success: { marginBottom: 8, fontSize: 14 },
  submitBtn: { marginBottom: 24 },
  historyTitle: { fontSize: 16, fontWeight: '700', marginBottom: 12 },
  empty: { padding: 24, marginBottom: 16 },
  emptyText: { fontSize: 14 },
  feedbackCard: { padding: 14, marginBottom: 10 },
  feedbackHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 4 },
  feedbackType: { fontSize: 12, fontWeight: '700' },
  feedbackDate: { fontSize: 12 },
  feedbackSubject: { fontSize: 15, fontWeight: '600', marginBottom: 4 },
  feedbackMessage: { fontSize: 14 },
});
