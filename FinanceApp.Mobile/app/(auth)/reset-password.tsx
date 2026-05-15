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
import { router, useLocalSearchParams } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { useTheme } from '../../src/context/ThemeContext';
import { Input } from '../../src/components/Input';
import { Button } from '../../src/components/Button';
import { resetPassword } from '../../src/api/auth';
import { ApiError } from '../../src/api/client';

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/**
 * Accepts `email` and `code` from either:
 *   • Route params when navigated from the in-app forgot-password screen, or
 *   • A deep link, e.g. financeapp:///(auth)/reset-password?email=…&code=…
 *
 * The `code` field is always the base64url-encoded token produced by the API
 * (matches what FinanceApp.Web/Identity ResetPassword expects). Users coming
 * from the standard email link end up on the Web reset page; the mobile
 * screen is the "paste the code" fallback for users who prefer to finish the
 * reset on their phone or who use a deep-link build.
 */
export default function ResetPasswordScreen() {
  const { colors } = useTheme();
  const { t } = useTranslation();
  const params = useLocalSearchParams<{ email?: string; code?: string }>();

  const initialEmail = typeof params.email === 'string' ? params.email : '';
  const initialCode = typeof params.code === 'string' ? params.code : '';

  const [email, setEmail] = useState(initialEmail);
  const [code, setCode] = useState(initialCode);
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [done, setDone] = useState(false);

  const handleSubmit = async () => {
    setError('');
    const trimmedEmail = email.trim();
    const trimmedCode = code.trim();

    if (!trimmedEmail) {
      setError(t('auth.resetPassword.errors.emailRequired'));
      return;
    }
    if (!EMAIL_RE.test(trimmedEmail)) {
      setError(t('auth.resetPassword.errors.emailInvalid'));
      return;
    }
    if (!trimmedCode) {
      setError(t('auth.resetPassword.errors.codeRequired'));
      return;
    }
    if (!password) {
      setError(t('auth.resetPassword.errors.passwordRequired'));
      return;
    }
    if (password.length < 6) {
      setError(t('auth.resetPassword.errors.passwordShort'));
      return;
    }
    if (password !== confirm) {
      setError(t('auth.resetPassword.errors.passwordMismatch'));
      return;
    }

    setLoading(true);
    try {
      await resetPassword({
        email: trimmedEmail,
        code: trimmedCode,
        newPassword: password,
      });
      setDone(true);
    } catch (e) {
      if (e instanceof ApiError) {
        setError(e.message);
      } else {
        const msg = e instanceof Error ? e.message : t('auth.resetPassword.errors.generic');
        setError(
          msg.includes('fetch') || msg.includes('Network')
            ? t('auth.resetPassword.errors.network')
            : msg
        );
      }
    } finally {
      setLoading(false);
    }
  };

  const instruction = email.trim()
    ? t('auth.resetPassword.instruction', { email: email.trim() })
    : t('auth.resetPassword.instructionNoEmail');

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
          accessibilityLabel={t('auth.resetPassword.signIn')}
        >
          <Text style={[styles.backArrow, { color: colors.text.primary }]}>←</Text>
        </TouchableOpacity>

        <View style={[styles.iconWrap, { backgroundColor: colors.brandLight ?? '#EFF6FF' }]}>
          <Text style={styles.lockIcon}>{done ? '✅' : '🔑'}</Text>
        </View>
        <Text style={[styles.title, { color: colors.text.primary }]}>
          {done ? t('auth.resetPassword.successTitle') : t('auth.resetPassword.title')}
        </Text>
        <Text style={[styles.instruction, { color: colors.text.muted }]}>
          {done ? t('auth.resetPassword.successBody') : instruction}
        </Text>

        {!done ? (
          <View style={styles.form}>
            <Input
              label={t('auth.resetPassword.emailLabel')}
              value={email}
              onChangeText={(v) => {
                setEmail(v);
                if (error) setError('');
              }}
              placeholder={t('auth.resetPassword.emailPlaceholder')}
              keyboardType="email-address"
              autoCapitalize="none"
              autoCorrect={false}
              editable={!loading}
            />
            <Input
              label={t('auth.resetPassword.codeLabel')}
              value={code}
              onChangeText={(v) => {
                setCode(v);
                if (error) setError('');
              }}
              placeholder={t('auth.resetPassword.codePlaceholder')}
              autoCapitalize="none"
              autoCorrect={false}
              editable={!loading}
              multiline
            />
            <Text style={[styles.codeHelp, { color: colors.text.muted }]}>
              {t('auth.resetPassword.codeHelp')}
            </Text>
            <Input
              label={t('auth.resetPassword.passwordLabel')}
              value={password}
              onChangeText={(v) => {
                setPassword(v);
                if (error) setError('');
              }}
              placeholder={t('auth.resetPassword.passwordPlaceholder')}
              secureTextEntry
              editable={!loading}
              autoCapitalize="none"
            />
            <Input
              label={t('auth.resetPassword.confirmLabel')}
              value={confirm}
              onChangeText={(v) => {
                setConfirm(v);
                if (error) setError('');
              }}
              placeholder={t('auth.resetPassword.confirmPlaceholder')}
              secureTextEntry
              editable={!loading}
              autoCapitalize="none"
            />
            {error ? (
              <View style={[styles.errorCard, { backgroundColor: `${colors.danger}10` }]}>
                <Text style={[styles.errText, { color: colors.danger }]}>{error}</Text>
              </View>
            ) : null}
            <Button
              title={t('auth.resetPassword.submit')}
              onPress={handleSubmit}
              loading={loading}
              style={styles.btn}
            />
          </View>
        ) : (
          <View style={styles.form}>
            <Button
              title={t('auth.resetPassword.signIn')}
              onPress={() => router.replace('/(auth)/login')}
              style={styles.btn}
            />
          </View>
        )}

        {!done ? (
          <TouchableOpacity
            style={styles.backToLogin}
            onPress={() => router.replace('/(auth)/login')}
          >
            <Text style={[styles.backToLoginText, { color: colors.brand }]}>
              ← {t('auth.resetPassword.signIn')}
            </Text>
          </TouchableOpacity>
        ) : null}
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
  errorCard: { borderRadius: 10, padding: 12, marginBottom: 12 },
  errText: { fontSize: 14 },
  btn: { marginTop: 8 },
  codeHelp: { fontSize: 12, marginTop: -10, marginBottom: 12, lineHeight: 18 },
  backToLogin: { alignSelf: 'center', paddingVertical: 12 },
  backToLoginText: { fontSize: 15, fontWeight: '600' },
});
