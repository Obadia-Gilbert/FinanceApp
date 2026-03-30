import { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Switch,
  Alert,
  Image,
} from 'react-native';
import { useRouter } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTheme } from '../../src/context/ThemeContext';
import { useAuth } from '../../src/context/AuthContext';
import { Card } from '../../src/components/Card';
import { Input } from '../../src/components/Input';
import { Button } from '../../src/components/Button';
import { getProfile, updateProfile } from '../../src/api/profile';
import { ApiError } from '../../src/api/client';
import { normalizeAppLanguage, setAppLanguage, type AppLanguage } from '../../src/i18n/i18n';

export default function ProfileScreen() {
  const { t, i18n: i18nInstance } = useTranslation();
  const { colors, setMode, isDark } = useTheme();
  const { user, signOut } = useAuth();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [error, setError] = useState('');

  const { data: profile } = useQuery({
    queryKey: ['profile'],
    queryFn: getProfile,
  });

  useEffect(() => {
    if (profile) {
      setFirstName(profile.firstName ?? '');
      setLastName(profile.lastName ?? '');
    }
  }, [profile]);

  useEffect(() => {
    if (!profile?.preferredLanguage) return;
    const p = normalizeAppLanguage(profile.preferredLanguage);
    const cur = normalizeAppLanguage(i18nInstance.language);
    if (p !== cur) void setAppLanguage(p);
  }, [profile?.preferredLanguage]);

  const updateMutation = useMutation({
    mutationFn: updateProfile,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['profile'] });
      setError('');
      setEditing(false);
    },
    onError: (e) => {
      setError(e instanceof ApiError ? e.message : t('profile.updateFailed'));
    },
  });

  const handleSave = () => {
    setError('');
    if (!firstName.trim() || firstName.trim().length < 2) {
      setError(t('profile.validationFirstName'));
      return;
    }
    if (!lastName.trim() || lastName.trim().length < 2) {
      setError(t('profile.validationLastName'));
      return;
    }
    updateMutation.mutate({ firstName: firstName.trim(), lastName: lastName.trim() });
  };

  const pickLanguage = (code: AppLanguage) => {
    void (async () => {
      await setAppLanguage(code);
      try {
        await updateProfile({ preferredLanguage: code });
        await queryClient.invalidateQueries({ queryKey: ['profile'] });
      } catch {
        /* server sync optional */
      }
    })();
  };

  const openLanguagePicker = () => {
    Alert.alert(
      t('profile.language'),
      undefined,
      [
        { text: t('profile.english'), onPress: () => pickLanguage('en') },
        { text: t('profile.swahili'), onPress: () => pickLanguage('sw') },
        { text: t('profile.spanish'), onPress: () => pickLanguage('es') },
        { text: t('profile.cancel'), style: 'cancel' },
      ],
      { cancelable: true }
    );
  };

  const handleSignOut = () => {
    Alert.alert(t('more.signOutConfirmTitle'), t('more.signOutConfirmMessage'), [
      { text: t('more.cancel'), style: 'cancel' },
      {
        text: t('more.signOut'),
        style: 'destructive',
        onPress: async () => {
          await signOut();
          router.replace('/(auth)/login');
        },
      },
    ]);
  };

  const curLang = normalizeAppLanguage(i18nInstance.language);
  const langLabel =
    curLang === 'sw' ? t('profile.swahili') : curLang === 'es' ? t('profile.spanish') : t('profile.english');

  const displayName = profile
    ? [profile.firstName, profile.lastName].filter(Boolean).join(' ') || 'Account'
    : user?.firstName && user?.lastName
      ? `${user.firstName} ${user.lastName}`
      : 'Account';
  const displayEmail = profile?.email ?? user?.email ?? '';
  const initial = (firstName || profile?.firstName || user?.firstName)?.[0] ?? displayEmail?.[0] ?? '?';

  return (
    <ScrollView
      style={[styles.container, { backgroundColor: colors.bg.alt }]}
      contentContainerStyle={styles.content}
    >
      {/* Profile header */}
      <View style={styles.profileHeader}>
        <View style={styles.avatarWrap}>
          <View style={[styles.avatar, { backgroundColor: colors.brand }]}>
            <Text style={[styles.avatarText, { color: isDark ? '#0F172A' : '#fff' }]}>
              {initial}
            </Text>
          </View>
          <TouchableOpacity
            style={[styles.editAvatarBtn, { backgroundColor: colors.brand }]}
            onPress={() => setEditing(!editing)}
          >
            <Text style={[styles.editAvatarIcon, { color: isDark ? '#0F172A' : '#fff' }]}>✎</Text>
          </TouchableOpacity>
        </View>
        <Text style={[styles.displayName, { color: colors.text.primary }]}>{displayName}</Text>
        <Text style={[styles.displayEmail, { color: colors.text.muted }]}>{displayEmail}</Text>
      </View>

      {editing && (
        <Card style={styles.editCard}>
          <Input label={t('profile.firstName')} value={firstName} onChangeText={setFirstName} placeholder={t('profile.firstName')} />
          <Input label={t('profile.lastName')} value={lastName} onChangeText={setLastName} placeholder={t('profile.lastName')} />
          {error ? (
            <View style={[styles.errorCard, { backgroundColor: `${colors.danger}10` }]}>
              <Text style={[styles.err, { color: colors.danger }]}>{error}</Text>
            </View>
          ) : null}
          <View style={styles.editBtns}>
            <Button title={t('profile.cancel')} onPress={() => setEditing(false)} variant="ghost" style={styles.editBtn} />
            <Button title={t('profile.save')} onPress={handleSave} loading={updateMutation.isPending} style={styles.editBtn} />
          </View>
        </Card>
      )}

      {/* Account Management */}
      <Text style={[styles.sectionLabel, { color: colors.text.muted }]}>{t('profile.accountManagement')}</Text>
      <Card style={styles.menuCard}>
        <TouchableOpacity style={[styles.menuRow, { borderBottomColor: colors.border }]} onPress={() => setEditing(true)}>
          <View style={[styles.menuIconWrap, { backgroundColor: `${colors.brand}15` }]}>
            <Text style={styles.menuEmoji}>👤</Text>
          </View>
          <Text style={[styles.menuLabel, { color: colors.text.primary }]}>{t('profile.accountSettings')}</Text>
          <Text style={[styles.menuArrow, { color: colors.text.subtle }]}>›</Text>
        </TouchableOpacity>
        <TouchableOpacity style={[styles.menuRow, { borderBottomColor: colors.border }]}>
          <View style={[styles.menuIconWrap, { backgroundColor: `${colors.success}15` }]}>
            <Text style={styles.menuEmoji}>💵</Text>
          </View>
          <View style={styles.menuLabelWrap}>
            <Text style={[styles.menuLabel, { color: colors.text.primary }]}>{t('profile.currency')}</Text>
            <Text style={[styles.menuSub, { color: colors.text.muted }]}>{t('profile.currencyHint')}</Text>
          </View>
          <Text style={[styles.menuArrow, { color: colors.text.subtle }]}>›</Text>
        </TouchableOpacity>
        <TouchableOpacity style={styles.menuRow} onPress={() => router.push('/(tabs)/privacy')}>
          <View style={[styles.menuIconWrap, { backgroundColor: `${colors.info}15` }]}>
            <Text style={styles.menuEmoji}>🛡</Text>
          </View>
          <Text style={[styles.menuLabel, { color: colors.text.primary }]}>{t('profile.securityPrivacy')}</Text>
          <Text style={[styles.menuArrow, { color: colors.text.subtle }]}>›</Text>
        </TouchableOpacity>
      </Card>

      {/* App Preferences */}
      <Text style={[styles.sectionLabel, { color: colors.text.muted }]}>{t('profile.appPreferences')}</Text>
      <Card style={styles.menuCard}>
        <TouchableOpacity
          style={[styles.menuRow, { borderBottomColor: colors.border }]}
          onPress={openLanguagePicker}
          activeOpacity={0.7}
        >
          <View style={[styles.menuIconWrap, { backgroundColor: `${colors.brand}15` }]}>
            <Text style={styles.menuEmoji}>🌐</Text>
          </View>
          <View style={styles.menuLabelWrap}>
            <Text style={[styles.menuLabel, { color: colors.text.primary }]}>{t('profile.language')}</Text>
            <Text style={[styles.menuSub, { color: colors.text.muted }]}>{langLabel}</Text>
          </View>
          <Text style={[styles.menuArrow, { color: colors.text.subtle }]}>›</Text>
        </TouchableOpacity>
        <View style={[styles.menuRow, { borderBottomColor: colors.border }]}>
          <View style={[styles.menuIconWrap, { backgroundColor: `${colors.warning}15` }]}>
            <Text style={styles.menuEmoji}>🌙</Text>
          </View>
          <Text style={[styles.menuLabel, { color: colors.text.primary }]}>{t('profile.darkMode')}</Text>
          <Switch
            value={isDark}
            onValueChange={(v) => setMode(v ? 'dark' : 'light')}
            trackColor={{ false: colors.border, true: colors.brand }}
            thumbColor={isDark ? colors.bg.default : '#fff'}
          />
        </View>
        <TouchableOpacity style={[styles.menuRow, { borderBottomColor: colors.border }]} onPress={() => router.push('/(tabs)/notifications')}>
          <View style={[styles.menuIconWrap, { backgroundColor: `${colors.danger}15` }]}>
            <Text style={styles.menuEmoji}>🔔</Text>
          </View>
          <Text style={[styles.menuLabel, { color: colors.text.primary }]}>{t('profile.notifications')}</Text>
          <Text style={[styles.menuArrow, { color: colors.text.subtle }]}>›</Text>
        </TouchableOpacity>
      </Card>

      <TouchableOpacity style={[styles.signOut, { backgroundColor: colors.danger }]} onPress={handleSignOut} activeOpacity={0.8}>
        <Text style={styles.signOutText}>{t('profile.signOut')}</Text>
      </TouchableOpacity>

      <View style={styles.versionWrap}>
        <Image
          source={require('../../assets/logo.png')}
          style={styles.versionLogo}
          resizeMode="contain"
        />
        <Text style={[styles.version, { color: colors.text.muted }]}>{t('profile.version')}</Text>
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 16, paddingBottom: 40 },
  profileHeader: { alignItems: 'center', marginBottom: 24 },
  avatarWrap: { position: 'relative', marginBottom: 12 },
  avatar: { width: 80, height: 80, borderRadius: 40, justifyContent: 'center', alignItems: 'center' },
  avatarText: { fontSize: 32, fontWeight: '700' },
  editAvatarBtn: { position: 'absolute', right: -4, bottom: -4, width: 28, height: 28, borderRadius: 14, justifyContent: 'center', alignItems: 'center' },
  editAvatarIcon: { fontSize: 14 },
  displayName: { fontSize: 22, fontWeight: '700', marginBottom: 4 },
  displayEmail: { fontSize: 15 },
  editCard: { marginBottom: 24 },
  errorCard: { borderRadius: 10, padding: 12, marginBottom: 12 },
  editBtns: { flexDirection: 'row', gap: 12, marginTop: 8 },
  editBtn: { flex: 1 },
  err: { fontSize: 14 },
  sectionLabel: { fontSize: 12, fontWeight: '600', letterSpacing: 0.5, marginBottom: 8, marginLeft: 4 },
  menuCard: { marginBottom: 20, padding: 0, overflow: 'hidden' },
  menuRow: { flexDirection: 'row', alignItems: 'center', paddingVertical: 14, paddingHorizontal: 16, borderBottomWidth: 1 },
  menuIconWrap: {
    width: 36,
    height: 36,
    borderRadius: 10,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  menuEmoji: { fontSize: 18 },
  menuLabel: { flex: 1, fontSize: 16 },
  menuLabelWrap: { flex: 1 },
  menuSub: { fontSize: 13, marginTop: 2 },
  menuArrow: { fontSize: 18 },
  signOut: { paddingVertical: 14, borderRadius: 12, alignItems: 'center', marginTop: 8 },
  signOutText: { color: '#fff', fontSize: 16, fontWeight: '600' },
  versionWrap: { alignItems: 'center', marginTop: 28, gap: 8 },
  versionLogo: { width: 32, height: 32, borderRadius: 8 },
  version: { fontSize: 12 },
});
