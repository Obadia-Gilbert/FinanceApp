import { useState, useEffect } from 'react';
import {
  Modal,
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Switch,
  Alert,
  Image,
  TextInput,
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
import { AsYouType, getCountryCallingCode } from 'libphonenumber-js';
import { COUNTRY_OPTIONS, type CountryOption } from '../../src/utils/countryList';

export default function ProfileScreen() {
  const { t, i18n: i18nInstance } = useTranslation();
  const { colors, setMode, isDark } = useTheme();
  const { user, signOut } = useAuth();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [countryCode, setCountryCode] = useState<string>('');
  const [phoneDisplay, setPhoneDisplay] = useState<string>('');
  const [phoneNationalDigits, setPhoneNationalDigits] = useState<string>('');
  const [phoneSelection, setPhoneSelection] = useState<{ start: number; end: number }>({ start: 0, end: 0 });
  const [countryPickerOpen, setCountryPickerOpen] = useState(false);
  const [dailyReminderEnabled, setDailyReminderEnabled] = useState(true);
  const [updatingReminder, setUpdatingReminder] = useState(false);
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
    if (!profile) return;
    // Only sync these fields from the server when not actively editing.
    if (editing) return;

    const nextCountry = profile.countryCode ?? '';
    setCountryCode(nextCountry);

    const nextRawPhone = profile.phoneNumber ?? '';
    const digitsOnly = nextRawPhone.replace(/\D/g, '');
    const callingCode = getCallingCodeSafe(nextCountry);
    const nationalDigits = callingCode ? stripCallingCode(digitsOnly, callingCode) : digitsOnly;

    setPhoneNationalDigits(nationalDigits);
    const formatted = formatPhonePretty(nextCountry, nationalDigits);
    setPhoneDisplay(formatted);
    setPhoneSelection({ start: formatted.length, end: formatted.length });
    setDailyReminderEnabled(profile.dailyReminderEnabled ?? true);
  }, [profile, editing]);

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
    const payload: Parameters<typeof updateProfile>[0] = {
      firstName: firstName.trim(),
      lastName: lastName.trim(),
      countryCode: countryCode ? countryCode : null,
      phoneNumber: phoneNationalDigits ? phoneDisplay.trim() : null,
    };

    updateMutation.mutate(payload);
  };

  function getCallingCodeSafe(countryIso: string): string | null {
    if (!countryIso) return null;
    try {
      return String(getCountryCallingCode(countryIso as any));
    } catch {
      return null;
    }
  }

  function stripCallingCode(digitsOnly: string, callingCode: string): string {
    if (!digitsOnly) return '';
    if (digitsOnly.startsWith(callingCode)) return digitsOnly.slice(callingCode.length);
    return digitsOnly;
  }

  function formatPhonePretty(countryIso: string, nationalDigits: string): string {
    const callingCode = getCallingCodeSafe(countryIso);
    if (!callingCode) return nationalDigits || '';

    const prefix = `+${callingCode}`;
    if (!nationalDigits) return `${prefix} `;

    try {
      const formatter = new AsYouType(countryIso as any);
      formatter.input(nationalDigits);
      const num = formatter.getNumber();
      return num ? num.formatInternational() : `${prefix} ${nationalDigits}`;
    } catch {
      return `${prefix} ${nationalDigits}`;
    }
  }

  function getCountryName(code: string): string {
    return COUNTRY_OPTIONS.find((c) => c.code === code)?.name ?? '';
  }

  const handleCountrySelected = (next: CountryOption) => {
    const nextCode = next.code;
    setCountryCode(nextCode);

    const digitsOnly = (phoneDisplay || '').replace(/\D/g, '');
    const callingCode = getCallingCodeSafe(nextCode);
    const nationalDigits = callingCode ? stripCallingCode(digitsOnly, callingCode) : digitsOnly;

    setPhoneNationalDigits(nationalDigits);
    const formatted = formatPhonePretty(nextCode, nationalDigits);
    setPhoneDisplay(formatted);
    setPhoneSelection({ start: formatted.length, end: formatted.length });
    setCountryPickerOpen(false);
  };

  const handlePhoneChange = (text: string) => {
    const selected = countryCode;
    const digitsOnly = (text || '').replace(/\D/g, '');

    const callingCode = getCallingCodeSafe(selected);
    const nationalDigits = callingCode ? stripCallingCode(digitsOnly, callingCode) : digitsOnly;

    setPhoneNationalDigits(nationalDigits);
    const formatted = selected ? formatPhonePretty(selected, nationalDigits) : digitsOnly;
    setPhoneDisplay(formatted);
    setPhoneSelection({ start: formatted.length, end: formatted.length });
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

  const toggleDailyReminder = (enabled: boolean) => {
    setDailyReminderEnabled(enabled);
    setUpdatingReminder(true);
    setError('');

    void (async () => {
      try {
        await updateProfile({ dailyReminderEnabled: enabled });
        await queryClient.invalidateQueries({ queryKey: ['profile'] });
      } catch (e) {
        setDailyReminderEnabled((prev) => !enabled);
        setError(e instanceof ApiError ? e.message : t('profile.updateFailed'));
      } finally {
        setUpdatingReminder(false);
      }
    })();
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

          {/* Country selector + pretty international phone */}
          <TouchableOpacity
            onPress={() => setCountryPickerOpen(true)}
            style={[
              styles.selectLike,
              {
                backgroundColor: colors.bg.default,
                borderColor: colors.border,
              },
            ]}
          >
            <Text style={[styles.selectLabel, { color: colors.text.body }]}>{t('profile.country')}</Text>
            <Text style={[styles.selectValue, { color: colors.text.primary }]}>
              {countryCode ? getCountryName(countryCode) : t('profile.selectCountry')}
            </Text>
          </TouchableOpacity>

          <Text style={[styles.label, { color: colors.text.body }]}>{t('profile.phoneNumber')}</Text>
          <TextInput
            value={phoneDisplay}
            onChangeText={handlePhoneChange}
            placeholder={t('profile.phonePlaceholder')}
            placeholderTextColor={colors.text.subtle}
            keyboardType="phone-pad"
            autoCapitalize="none"
            autoCorrect={false}
            selection={phoneSelection}
            style={[
              styles.phoneInput,
              {
                backgroundColor: colors.bg.default,
                borderColor: colors.border,
                color: colors.text.primary,
              },
            ]}
          />
          <Text style={[styles.hint, { color: colors.text.subtle }]}>{t('profile.phonePrefixHint')}</Text>

          <Modal visible={countryPickerOpen} transparent animationType="fade" onRequestClose={() => setCountryPickerOpen(false)}>
            <TouchableOpacity style={styles.modalBackdrop} activeOpacity={1} onPress={() => setCountryPickerOpen(false)}>
              <View style={[styles.countryModal, { backgroundColor: colors.bg.card, borderColor: colors.border }]}>
                <Text style={[styles.countryModalTitle, { color: colors.text.primary }]}>{t('profile.selectCountry')}</Text>
                <ScrollView style={styles.countryScroll}>
                  {COUNTRY_OPTIONS.map((c) => (
                    <TouchableOpacity
                      key={c.code || 'none'}
                      onPress={() => handleCountrySelected(c)}
                      style={[
                        styles.countryItem,
                        {
                          borderBottomColor: colors.border,
                          backgroundColor: c.code === countryCode ? `${colors.brand}15` : 'transparent',
                        },
                      ]}
                    >
                      <Text
                        style={{
                          color: c.code === countryCode ? colors.brand : colors.text.primary,
                          fontWeight: c.code === countryCode ? '700' : '500',
                        }}
                      >
                        {c.name}
                      </Text>
                    </TouchableOpacity>
                  ))}
                </ScrollView>
              </View>
            </TouchableOpacity>
          </Modal>

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
        <View style={[styles.menuRow, { borderBottomColor: colors.border }]}>
          <View style={[styles.menuIconWrap, { backgroundColor: `${colors.info}15` }]}>
            <Text style={styles.menuEmoji}>⏰</Text>
          </View>
          <View style={styles.menuLabelWrap}>
            <Text style={[styles.menuLabel, { color: colors.text.primary }]}>{t('profile.dailyReminderTitle')}</Text>
            <Text style={[styles.menuSub, { color: colors.text.muted }]}>{t('profile.dailyReminderHint')}</Text>
          </View>
          <Switch
            value={dailyReminderEnabled}
            onValueChange={toggleDailyReminder}
            disabled={updatingReminder}
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

  // Profile phone formatting UI
  selectLike: { borderWidth: 1, borderRadius: 12, paddingVertical: 12, paddingHorizontal: 14, marginBottom: 16 },
  selectLabel: { fontSize: 14, fontWeight: '500', marginBottom: 6 },
  selectValue: { fontSize: 16, fontWeight: '600' },
  label: { fontSize: 14, fontWeight: '500', marginTop: 6, marginBottom: 6 },
  phoneInput: { borderWidth: 1, borderRadius: 12, paddingHorizontal: 14, paddingVertical: 12, fontSize: 16, borderColor: '#E5E7EB' },
  hint: { fontSize: 12, marginTop: 6 },

  modalBackdrop: { flex: 1, backgroundColor: 'rgba(0,0,0,0.35)', justifyContent: 'center', alignItems: 'center', padding: 16 },
  countryModal: { width: '100%', maxHeight: 520, borderRadius: 14, borderWidth: 1, overflow: 'hidden' },
  countryModalTitle: { fontSize: 16, fontWeight: '700', paddingHorizontal: 16, paddingVertical: 12 },
  countryScroll: { maxHeight: 460 },
  countryItem: { paddingHorizontal: 16, paddingVertical: 14, borderBottomWidth: 1 },

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
