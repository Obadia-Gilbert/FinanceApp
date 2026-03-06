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
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTheme } from '../../src/context/ThemeContext';
import { Card } from '../../src/components/Card';
import { Button } from '../../src/components/Button';
import { Input } from '../../src/components/Input';
import { getMyFeedback, createFeedback } from '../../src/api/feedback';
import { ApiError } from '../../src/api/client';

const FEEDBACK_TYPES = [
  { label: '❓ Question', value: 0, emoji: '❓' },
  { label: '💡 Suggestion', value: 1, emoji: '💡' },
  { label: '💬 Comment', value: 2, emoji: '💬' },
];

export default function FeedbackScreen() {
  const { colors } = useTheme();
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
    if (!message.trim()) { setError('Please enter your message.'); return; }
    createMutation.mutate({ type, message: message.trim(), subject: subject.trim() || null });
  };

  return (
    <ScrollView
      style={[styles.container, { backgroundColor: colors.bg.alt }]}
      contentContainerStyle={styles.content}
      keyboardShouldPersistTaps="handled"
      refreshControl={<RefreshControl refreshing={isRefetching} onRefresh={() => refetch()} tintColor={colors.brand} />}
    >
      <Text style={[styles.sectionTitle, { color: colors.text.primary }]}>Send Feedback</Text>
      <Text style={[styles.hint, { color: colors.text.muted }]}>
        Questions, suggestions, or comments — we read them all.
      </Text>

      {/* Type selector */}
      <View style={styles.typeRow}>
        {FEEDBACK_TYPES.map((t) => (
          <TouchableOpacity
            key={t.value}
            onPress={() => setType(t.value)}
            style={[
              styles.typeChip,
              {
                backgroundColor: type === t.value ? colors.brand : colors.bg.default,
                borderColor: type === t.value ? colors.brand : colors.border,
              },
            ]}
            activeOpacity={0.7}
          >
            <Text style={[styles.typeChipText, { color: type === t.value ? '#fff' : colors.text.primary }]}>{t.label}</Text>
          </TouchableOpacity>
        ))}
      </View>

      <Input label="Subject (optional)" value={subject} onChangeText={setSubject} placeholder="Short title" />

      <View style={styles.field}>
        <Text style={[styles.label, { color: colors.text.body }]}>Message *</Text>
        <TextInput
          style={[
            styles.textArea,
            {
              color: colors.text.primary,
              borderColor: colors.border,
              backgroundColor: colors.bg.default,
            },
          ]}
          placeholder="Your feedback..."
          placeholderTextColor={colors.text.subtle}
          value={message}
          onChangeText={setMessage}
          multiline
          numberOfLines={4}
        />
      </View>

      {error ? (
        <View style={[styles.errorCard, { backgroundColor: `${colors.danger}10` }]}>
          <Text style={[styles.err, { color: colors.danger }]}>{error}</Text>
        </View>
      ) : null}
      {success ? (
        <View style={[styles.successCard, { backgroundColor: `${colors.success}10` }]}>
          <Text style={[styles.successText, { color: colors.success }]}>✓ Thanks! Your feedback was sent.</Text>
        </View>
      ) : null}

      <Button title="Send Feedback" onPress={handleSubmit} loading={createMutation.isPending} style={styles.submitBtn} />

      {/* History */}
      <View style={styles.historyHeader}>
        <Text style={[styles.historyTitle, { color: colors.text.primary }]}>Your Feedback</Text>
        <Text style={[styles.historyCount, { color: colors.text.muted }]}>{list.length} sent</Text>
      </View>

      {list.length === 0 ? (
        <View style={styles.emptyWrap}>
          <Text style={{ fontSize: 48, marginBottom: 12 }}>💬</Text>
          <Text style={[styles.emptyText, { color: colors.text.muted }]}>No feedback sent yet.</Text>
        </View>
      ) : (
        list.map((item) => (
          <Card key={item.id} style={styles.feedbackCard}>
            <View style={styles.feedbackHeader}>
              <View style={[styles.feedbackTypeBadge, { backgroundColor: `${colors.brand}15` }]}>
                <Text style={[styles.feedbackType, { color: colors.brand }]}>
                  {FEEDBACK_TYPES.find((t) => t.value === item.type)?.label ?? 'Feedback'}
                </Text>
              </View>
              <Text style={[styles.feedbackDate, { color: colors.text.subtle }]}>
                {item.createdAt ? new Date(item.createdAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }) : ''}
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
  content: { padding: 16, paddingBottom: 40 },
  sectionTitle: { fontSize: 20, fontWeight: '700', marginBottom: 4 },
  hint: { fontSize: 14, marginBottom: 20 },
  typeRow: { flexDirection: 'row', gap: 10, marginBottom: 16 },
  typeChip: { paddingHorizontal: 14, paddingVertical: 10, borderRadius: 10, borderWidth: 1 },
  typeChipText: { fontSize: 14, fontWeight: '600' },
  field: { marginBottom: 16 },
  label: { fontSize: 14, fontWeight: '500', marginBottom: 8 },
  textArea: {
    borderWidth: 1,
    borderRadius: 12,
    padding: 14,
    fontSize: 15,
    minHeight: 120,
    textAlignVertical: 'top',
  },
  errorCard: { borderRadius: 10, padding: 12, marginBottom: 12 },
  err: { fontSize: 14 },
  successCard: { borderRadius: 10, padding: 12, marginBottom: 12 },
  successText: { fontSize: 14, fontWeight: '500' },
  submitBtn: { marginBottom: 28 },
  historyHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 },
  historyTitle: { fontSize: 16, fontWeight: '700' },
  historyCount: { fontSize: 13 },
  emptyWrap: { alignItems: 'center', paddingVertical: 24 },
  emptyText: { fontSize: 14 },
  feedbackCard: { padding: 14, marginBottom: 10 },
  feedbackHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 },
  feedbackTypeBadge: { paddingHorizontal: 10, paddingVertical: 4, borderRadius: 6 },
  feedbackType: { fontSize: 12, fontWeight: '700' },
  feedbackDate: { fontSize: 12 },
  feedbackSubject: { fontSize: 15, fontWeight: '600', marginBottom: 4 },
  feedbackMessage: { fontSize: 14, lineHeight: 20 },
});
