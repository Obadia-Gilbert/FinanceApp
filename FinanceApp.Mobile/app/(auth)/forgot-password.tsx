import { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
} from 'react-native';
import { router } from 'expo-router';
import { useTheme } from '../../src/context/ThemeContext';
import { Input } from '../../src/components/Input';
import { Button } from '../../src/components/Button';

export default function ForgotPasswordScreen() {
  const { colors } = useTheme();
  const [email, setEmail] = useState('');
  const [sent, setSent] = useState(false);
  const [loading, setLoading] = useState(false);

  const handleSendReset = async () => {
    if (!email.trim()) return;
    setLoading(true);
    // TODO: call API when backend supports forgot-password
    await new Promise((r) => setTimeout(r, 800));
    setSent(true);
    setLoading(false);
  };

  return (
    <KeyboardAvoidingView
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      style={[styles.container, { backgroundColor: colors.bg.alt }]}
    >
      <ScrollView
        contentContainerStyle={styles.scroll}
        keyboardShouldPersistTaps="handled"
        showsVerticalScrollIndicator={false}
      >
        <TouchableOpacity
          style={[styles.backBtn, { backgroundColor: colors.bg.default }]}
          onPress={() => router.back()}
        >
          <Text style={[styles.backArrow, { color: colors.text.primary }]}>←</Text>
        </TouchableOpacity>

        <View style={[styles.iconWrap, { backgroundColor: colors.brandLight ?? '#EFF6FF' }]}>
          <Text style={styles.lockIcon}>🔐</Text>
        </View>
        <Text style={[styles.title, { color: colors.text.primary }]}>Reset Password</Text>
        <Text style={[styles.instruction, { color: colors.text.muted }]}>
          Enter your email address and we will send you a link to reset your password.
        </Text>

        <View style={styles.form}>
          <Input
            label="Email Address"
            value={email}
            onChangeText={setEmail}
            placeholder="name@example.com"
            keyboardType="email-address"
            autoCapitalize="none"
            editable={!sent}
          />
          {sent ? (
            <Text style={[styles.successText, { color: colors.success }]}>
              If an account exists for this email, you will receive a reset link.
            </Text>
          ) : null}
          <Button
            title="Send Reset Link"
            onPress={handleSendReset}
            loading={loading}
            style={styles.btn}
          />
        </View>

        <TouchableOpacity style={styles.backToLogin} onPress={() => router.replace('/(auth)/login')}>
          <Text style={[styles.backToLoginText, { color: colors.brand }]}>← Back to Login</Text>
        </TouchableOpacity>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  scroll: {
    flexGrow: 1,
    paddingHorizontal: 24,
    paddingTop: 56,
    paddingBottom: 40,
  },
  backBtn: {
    width: 40,
    height: 40,
    borderRadius: 20,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 24,
  },
  backArrow: { fontSize: 20, fontWeight: '600' },
  iconWrap: {
    width: 80,
    height: 80,
    borderRadius: 40,
    justifyContent: 'center',
    alignItems: 'center',
    alignSelf: 'center',
    marginBottom: 20,
  },
  lockIcon: { fontSize: 36 },
  title: { fontSize: 24, fontWeight: '700', textAlign: 'center', marginBottom: 12 },
  instruction: { fontSize: 15, textAlign: 'center', marginBottom: 28, lineHeight: 22 },
  form: { marginBottom: 24 },
  successText: { fontSize: 14, marginBottom: 16 },
  btn: { marginTop: 8 },
  backToLogin: { alignSelf: 'center', paddingVertical: 12 },
  backToLoginText: { fontSize: 15, fontWeight: '600' },
});
