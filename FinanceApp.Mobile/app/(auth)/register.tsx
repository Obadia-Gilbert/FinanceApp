import { useState } from 'react';
import {
  View,
  Text,
  Image,
  StyleSheet,
  TouchableOpacity,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
} from 'react-native';
import { Link, router } from 'expo-router';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useTheme } from '../../src/context/ThemeContext';
import { Input } from '../../src/components/Input';
import { Button } from '../../src/components/Button';
import { register } from '../../src/api/auth';
import { ApiError } from '../../src/api/client';

function getPasswordStrength(pwd: string): { level: number; label: string; color: string } {
  if (!pwd.length) return { level: 0, label: '', color: '' };
  let score = 0;
  if (pwd.length >= 8) score++;
  if (pwd.length >= 12) score++;
  if (/[a-z]/.test(pwd) && /[A-Z]/.test(pwd)) score++;
  if (/\d/.test(pwd)) score++;
  if (/[^a-zA-Z0-9]/.test(pwd)) score++;
  if (score <= 1) return { level: 1, label: 'Weak', color: '#EF4444' };
  if (score <= 3) return { level: 2, label: 'Medium', color: '#F59E0B' };
  return { level: 3, label: 'Strong', color: '#10B981' };
}

export default function RegisterScreen() {
  const { colors } = useTheme();
  const insets = useSafeAreaInsets();
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [agreeTerms, setAgreeTerms] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const strength = getPasswordStrength(password);

  const handleRegister = async () => {
    setError('');
    if (!firstName.trim() || firstName.trim().length < 2) { setError('First name must be at least 2 characters'); return; }
    if (!lastName.trim() || lastName.trim().length < 2) { setError('Last name must be at least 2 characters'); return; }
    if (!email.trim()) { setError('Email is required'); return; }
    if (!password || password.length < 6) { setError('Password must be at least 6 characters'); return; }
    if (!agreeTerms) { setError('Please agree to the Terms of Service and Privacy Policy'); return; }
    setLoading(true);
    try {
      await register({
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        email: email.trim(),
        password,
      });
      router.replace('/(tabs)');
    } catch (e) {
      if (e instanceof ApiError) {
        setError(e.message);
      } else {
        const msg = e instanceof Error ? e.message : 'Something went wrong. Please try again.';
        setError(msg.includes('fetch') || msg.includes('Network') ? 'Cannot reach the server. Check Wi‑Fi and API URL in .env.' : msg);
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <KeyboardAvoidingView
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      style={[styles.container, { backgroundColor: colors.bg.alt }]}
    >
      <ScrollView
        contentContainerStyle={[styles.scroll, { paddingTop: insets.top + 16, paddingBottom: insets.bottom + 24 }]}
        keyboardShouldPersistTaps="handled"
        showsVerticalScrollIndicator={false}
      >
        <TouchableOpacity
          onPress={() => router.back()}
          style={styles.backRow}
          activeOpacity={0.7}
          accessibilityLabel="Back to Sign In"
        >
          <Text style={[styles.backArrow, { color: colors.brand }]}>←</Text>
          <Text style={[styles.backText, { color: colors.brand }]}>Back to Sign In</Text>
        </TouchableOpacity>

        <View style={styles.logoSection}>
          <View style={styles.logoWrap}>
            <Image
              source={require('../../assets/logo.png')}
              style={styles.logo}
              resizeMode="contain"
            />
          </View>
        </View>
        <Text style={[styles.title, { color: colors.text.primary }]}>Create Account</Text>
        <Text style={[styles.subtitle, { color: colors.text.muted }]}>
          Join FinanceApp to track your spending.
        </Text>

        <View style={styles.form}>
          <View style={styles.nameRow}>
            <View style={styles.nameHalf}>
              <Input label="First name" value={firstName} onChangeText={setFirstName} placeholder="John" />
            </View>
            <View style={styles.nameHalf}>
              <Input label="Last name" value={lastName} onChangeText={setLastName} placeholder="Doe" />
            </View>
          </View>
          <Input
            label="Email"
            value={email}
            onChangeText={setEmail}
            placeholder="name@example.com"
            keyboardType="email-address"
            autoCapitalize="none"
          />
          <Input
            label="Password"
            value={password}
            onChangeText={setPassword}
            placeholder="Create a strong password"
            secureTextEntry
          />
          {password.length > 0 && (
            <View style={styles.strengthWrap}>
              <View style={styles.strengthBar}>
                {[1, 2, 3].map((i) => (
                  <View
                    key={i}
                    style={[
                      styles.strengthSegment,
                      { backgroundColor: colors.border },
                      i <= strength.level && { backgroundColor: strength.color },
                    ]}
                  />
                ))}
              </View>
              <Text style={[styles.strengthText, { color: colors.text.muted }]}>
                Password strength: <Text style={{ color: strength.color }}>{strength.label}</Text>
              </Text>
            </View>
          )}
          <View style={styles.termsRow}>
            <TouchableOpacity
              onPress={() => setAgreeTerms(!agreeTerms)}
              activeOpacity={0.7}
              style={styles.checkboxTouch}
            >
              <View
                style={[
                  styles.checkbox,
                  { borderColor: colors.text.subtle },
                  agreeTerms && { backgroundColor: colors.brand, borderColor: colors.brand },
                ]}
              >
                {agreeTerms ? <Text style={styles.check}>✓</Text> : null}
              </View>
            </TouchableOpacity>
            <Text style={[styles.termsText, { color: colors.text.body }]}>
              I agree to the{' '}
              <Text style={[styles.termsLink, { color: colors.brand }]} onPress={() => router.push('/(auth)/privacy')}>
                Terms of Service
              </Text>
              {' '}and{' '}
              <Text style={[styles.termsLink, { color: colors.brand }]} onPress={() => router.push('/(auth)/privacy')}>
                Privacy Policy
              </Text>
            </Text>
          </View>
          {error ? (
            <View style={[styles.errorCard, { backgroundColor: `${colors.danger}10` }]}>
              <Text style={[styles.errText, { color: colors.danger }]}>{error}</Text>
            </View>
          ) : null}
          <Button title="Create Account" onPress={handleRegister} loading={loading} style={styles.btn} />
        </View>

        <View style={styles.divider}>
          <View style={[styles.dividerLine, { backgroundColor: colors.border }]} />
          <Text style={[styles.dividerText, { color: colors.text.muted }]}>OR CONTINUE WITH</Text>
          <View style={[styles.dividerLine, { backgroundColor: colors.border }]} />
        </View>
        <View style={styles.socialRow}>
          <TouchableOpacity
            style={[styles.socialBtn, { backgroundColor: colors.bg.default, borderColor: colors.border }]}
            onPress={() => {}}
          >
            <Text style={[styles.socialIcon, { color: colors.text.primary }]}>G</Text>
            <Text style={[styles.socialLabel, { color: colors.text.primary }]}>Google</Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.socialBtn, { backgroundColor: colors.bg.default, borderColor: colors.border }]}
            onPress={() => {}}
          >
            <Text style={styles.socialIcon}>🍎</Text>
            <Text style={[styles.socialLabel, { color: colors.text.primary }]}>Apple</Text>
          </TouchableOpacity>
        </View>

        <View style={styles.footer}>
          <Text style={[styles.footText, { color: colors.text.muted }]}>
            Already have an account?{' '}
          </Text>
          <Link href="/(auth)/login" asChild>
            <TouchableOpacity>
              <Text style={[styles.link, { color: colors.brand }]}>Log In</Text>
            </TouchableOpacity>
          </Link>
        </View>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  scroll: {
    flexGrow: 1,
    paddingHorizontal: 24,
  },
  backRow: {
    flexDirection: 'row',
    alignItems: 'center',
    alignSelf: 'flex-start',
    marginBottom: 20,
    paddingVertical: 8,
    paddingRight: 12,
  },
  backArrow: { fontSize: 22, marginRight: 6, fontWeight: '600' },
  backText: { fontSize: 16, fontWeight: '600' },
  logoSection: { alignItems: 'center', marginBottom: 16 },
  logoWrap: {
    width: 72,
    height: 72,
    borderRadius: 18,
    overflow: 'hidden',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 6,
    elevation: 4,
  },
  logo: { width: 72, height: 72 },
  title: { fontSize: 24, fontWeight: '700', textAlign: 'center', marginBottom: 8 },
  subtitle: { fontSize: 15, textAlign: 'center', marginBottom: 24 },
  form: { marginBottom: 20 },
  nameRow: { flexDirection: 'row', gap: 12 },
  nameHalf: { flex: 1 },
  strengthWrap: { marginBottom: 16 },
  strengthBar: { flexDirection: 'row', gap: 4, marginBottom: 6 },
  strengthSegment: { flex: 1, height: 4, borderRadius: 2 },
  strengthText: { fontSize: 13 },
  termsRow: { flexDirection: 'row', alignItems: 'flex-start', marginBottom: 20 },
  checkboxTouch: { marginRight: 12 },
  checkbox: {
    width: 22,
    height: 22,
    borderRadius: 6,
    borderWidth: 2,
    marginTop: 2,
    justifyContent: 'center',
    alignItems: 'center',
  },
  check: { color: '#fff', fontSize: 14, fontWeight: '700' },
  termsText: { flex: 1, fontSize: 14 },
  termsLink: { fontWeight: '600' },
  errorCard: { borderRadius: 10, padding: 12, marginBottom: 12 },
  errText: { fontSize: 14 },
  btn: { marginTop: 4 },
  divider: { flexDirection: 'row', alignItems: 'center', marginBottom: 20 },
  dividerLine: { flex: 1, height: 1 },
  dividerText: { fontSize: 12, marginHorizontal: 12 },
  socialRow: { flexDirection: 'row', gap: 12 },
  socialBtn: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    paddingVertical: 14,
    borderRadius: 12,
    borderWidth: 1,
  },
  socialIcon: { fontSize: 18 },
  socialLabel: { fontSize: 15, fontWeight: '500' },
  footer: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    marginTop: 28,
    paddingTop: 16,
  },
  footText: { fontSize: 15 },
  link: { fontSize: 15, fontWeight: '600' },
});
