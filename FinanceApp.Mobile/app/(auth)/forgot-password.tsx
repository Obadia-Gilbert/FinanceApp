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
import { useTranslation } from 'react-i18next';
import { useTheme } from '../../src/context/ThemeContext';
import { Input } from '../../src/components/Input';
import { Button } from '../../src/components/Button';
import { forgotPassword } from '../../src/api/auth';
import { ApiError } from '../../src/api/client';

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export default function ForgotPasswordScreen() {
  const { colors } = useTheme();
  const { t } = useTranslation();
  const [email, setEmail] = useState('');
  const [sent, setSent] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSendReset = async () => {
    setError('');
    const trimmed = email.trim();
    if (!trimmed) {
      setError(t('auth.forgotPassword.errors.emailRequired'));
      return;
    }
    if (!EMAIL_RE.test(trimmed)) {
      setError(t('auth.forgotPassword.errors.emailInvalid'));
      return;
    }
    setLoading(true);
    try {
      await forgotPassword(trimmed);
      setSent(true);
    } catch (e) {
      if (e instanceof ApiError) {
        setError(e.message);
      } else {
        const msg = e instanceof Error ? e.message : t('auth.forgotPassword.errors.generic');
        setError(
          msg.includes('fetch') || msg.includes('Network')
            ? t('auth.forgotPassword.errors.network')
            : msg
        );
      }
    } finally {
      setLoading(false);
    }
  };

  const handleResend = () => {
    setSent(false);
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
          accessibilityLabel={t('auth.forgotPassword.backToLogin')}
        >
          <Text style={[styles.backArrow, { color: colors.text.primary }]}>←</Text>
        </TouchableOpacity>

        <View style={[styles.iconWrap, { backgroundColor: colors.brandLight ?? '#EFF6FF' }]}>
          <Text style={styles.lockIcon}>{sent ? '✉️' : '🔐'}</Text>
        </View>
        <Text style={[styles.title, { color: colors.text.primary }]}>
          {sent ? t('auth.forgotPassword.sentTitle') : t('auth.forgotPassword.title')}
        </Text>
        <Text style={[styles.instruction, { color: colors.text.muted }]}>
          {sent
            ? t('auth.forgotPassword.sentBody', { email: email.trim() })
            : t('auth.forgotPassword.instruction')}
        </Text>

        {!sent ? (
          <View style={styles.form}>
            <Input
              label={t('auth.forgotPassword.emailLabel')}
              value={email}
              onChangeText={(v) => {
                setEmail(v);
                if (error) setError('');
              }}
              placeholder={t('auth.forgotPassword.emailPlaceholder')}
              keyboardType="email-address"
              autoCapitalize="none"
              autoCorrect={false}
              editable={!loading}
            />
            {error ? (
              <View style={[styles.errorCard, { backgroundColor: `${colors.danger}10` }]}>
                <Text style={[styles.errText, { color: colors.danger }]}>{error}</Text>
              </View>
            ) : null}
            <Button
              title={t('auth.forgotPassword.submit')}
              onPress={handleSendReset}
              loading={loading}
              style={styles.btn}
            />
            <TouchableOpacity
              style={styles.codeLinkWrap}
              onPress={() =>
                router.push({
                  pathname: '/(auth)/reset-password',
                  params: email.trim() ? { email: email.trim() } : undefined,
                })
              }
              accessibilityLabel={t('auth.forgotPassword.haveCode')}
            >
              <Text style={[styles.codeLinkText, { color: colors.brand }]}>
                {t('auth.forgotPassword.haveCode')}
              </Text>
            </TouchableOpacity>
          </View>
        ) : (
          <View style={styles.form}>
            <Text style={[styles.tip, { color: colors.text.muted }]}>
              {t('auth.forgotPassword.tip')}
            </Text>
            <Button
              title={t('auth.forgotPassword.backToLogin')}
              onPress={() => router.replace('/(auth)/login')}
              style={styles.btn}
            />
            <TouchableOpacity
              style={styles.codeLinkWrap}
              onPress={() =>
                router.push({
                  pathname: '/(auth)/reset-password',
                  params: email.trim() ? { email: email.trim() } : undefined,
                })
              }
              accessibilityLabel={t('auth.forgotPassword.haveCode')}
            >
              <Text style={[styles.codeLinkText, { color: colors.brand }]}>
                {t('auth.forgotPassword.haveCode')}
              </Text>
            </TouchableOpacity>
            <TouchableOpacity
              style={styles.resendWrap}
              onPress={handleResend}
              accessibilityLabel={t('auth.forgotPassword.resend')}
            >
              <Text style={[styles.resendText, { color: colors.brand }]}>
                {t('auth.forgotPassword.resend')}
              </Text>
            </TouchableOpacity>
          </View>
        )}

        {!sent ? (
          <TouchableOpacity
            style={styles.backToLogin}
            onPress={() => router.replace('/(auth)/login')}
          >
            <Text style={[styles.backToLoginText, { color: colors.brand }]}>
              ← {t('auth.forgotPassword.backToLogin')}
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
  tip: { fontSize: 14, lineHeight: 20, marginBottom: 16 },
  resendWrap: { alignSelf: 'center', paddingVertical: 12, marginTop: 4 },
  resendText: { fontSize: 14, fontWeight: '500' },
  codeLinkWrap: { alignSelf: 'center', paddingVertical: 12, marginTop: 4 },
  codeLinkText: { fontSize: 14, fontWeight: '500' },
  backToLogin: { alignSelf: 'center', paddingVertical: 12 },
  backToLoginText: { fontSize: 15, fontWeight: '600' },
});
