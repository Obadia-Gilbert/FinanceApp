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
import { login } from '../../src/api/auth';
import { ApiError } from '../../src/api/client';

export default function LoginScreen() {
  const { colors, isDark } = useTheme();
  const insets = useSafeAreaInsets();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleLogin = async () => {
    setError('');
    if (!email.trim()) { setError('Email is required'); return; }
    if (!password) { setError('Password is required'); return; }
    setLoading(true);
    try {
      await login({ email: email.trim(), password });
      router.replace('/(tabs)');
    } catch (e) {
      if (e instanceof ApiError) {
        setError(e.message);
      } else {
        const msg = e instanceof Error ? e.message : 'Something went wrong. Please try again.';
        setError(msg.includes('fetch') || msg.includes('Network') ? 'Cannot reach the server. Check Wi‑Fi and that the API is running at the URL in .env.' : msg);
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
        contentContainerStyle={[styles.scroll, { paddingTop: insets.top + 24, paddingBottom: insets.bottom + 24 }]}
        keyboardShouldPersistTaps="handled"
        showsVerticalScrollIndicator={false}
      >
        {/* Welcome card with logo */}
        <View style={[styles.welcomeCard, { backgroundColor: colors.brandLight ?? colors.bg.default }]}>
          <View style={styles.logoWrap}>
            <Image
              source={require('../../assets/logo.png')}
              style={styles.logo}
              resizeMode="contain"
            />
          </View>
          <Text style={[styles.welcomeTitle, { color: colors.text.primary }]}>Welcome Back</Text>
          <Text style={[styles.welcomeSubtitle, { color: colors.text.muted }]}>
            Manage your finances with ease.
          </Text>
        </View>

        <View style={styles.form}>
          <Input
            label="Email"
            value={email}
            onChangeText={setEmail}
            placeholder="name@example.com"
            keyboardType="email-address"
            autoCapitalize="none"
            autoCorrect={false}
          />
          <Input
            label="Password"
            value={password}
            onChangeText={setPassword}
            placeholder="Enter your password"
            secureTextEntry={!showPassword}
          />
          <TouchableOpacity
            style={styles.forgotWrap}
            onPress={() => router.push('/(auth)/forgot-password')}
          >
            <Text style={[styles.forgotLink, { color: colors.brand }]}>Forgot Password?</Text>
          </TouchableOpacity>
          {error ? (
            <View style={[styles.errorCard, { backgroundColor: `${colors.danger}10` }]}>
              <Text style={[styles.errText, { color: colors.danger }]}>{error}</Text>
            </View>
          ) : null}
          <Button title="Sign In" onPress={handleLogin} loading={loading} style={styles.btn} />
        </View>

        <View style={styles.divider}>
          <View style={[styles.dividerLine, { backgroundColor: colors.border }]} />
          <Text style={[styles.dividerText, { color: colors.text.muted }]}>Or continue with</Text>
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
            <Text style={[styles.socialIcon, { color: colors.text.primary }]}>f</Text>
            <Text style={[styles.socialLabel, { color: colors.text.primary }]}>Facebook</Text>
          </TouchableOpacity>
        </View>

        <View style={styles.footer}>
          <Text style={[styles.footText, { color: colors.text.muted }]}>
            Don't have an account?{' '}
          </Text>
          <Link href="/(auth)/register" asChild>
            <TouchableOpacity>
              <Text style={[styles.link, { color: colors.brand }]}>Sign Up</Text>
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
  welcomeCard: {
    borderRadius: 20,
    paddingVertical: 32,
    paddingHorizontal: 24,
    alignItems: 'center',
    marginBottom: 28,
  },
  logoWrap: {
    width: 72,
    height: 72,
    borderRadius: 18,
    overflow: 'hidden',
    marginBottom: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 6,
    elevation: 4,
  },
  logo: { width: 72, height: 72 },
  welcomeTitle: { fontSize: 24, fontWeight: '700', marginBottom: 6 },
  welcomeSubtitle: { fontSize: 15 },
  form: { marginBottom: 20 },
  forgotWrap: { alignSelf: 'flex-end', marginTop: -8, marginBottom: 16 },
  forgotLink: { fontSize: 14, fontWeight: '500' },
  errorCard: { borderRadius: 10, padding: 12, marginBottom: 12 },
  errText: { fontSize: 14 },
  btn: { marginTop: 4 },
  divider: { flexDirection: 'row', alignItems: 'center', marginBottom: 20 },
  dividerLine: { flex: 1, height: 1 },
  dividerText: { fontSize: 13, marginHorizontal: 12 },
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
  socialIcon: { fontSize: 18, fontWeight: '700' },
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
