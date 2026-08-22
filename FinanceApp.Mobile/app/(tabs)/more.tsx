import { View, Text, StyleSheet, ScrollView, TouchableOpacity, Alert } from 'react-native';
import { useRouter } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { useTheme } from '../../src/context/ThemeContext';
import { useAuth } from '../../src/context/AuthContext';
import { Card } from '../../src/components/Card';
import { Icon, type IconName } from '../../src/components/Icon';
import { getProfile } from '../../src/api/profile';
import { getSubscription } from '../../src/api/subscription';

type MenuItem = { label: string; href: string; icon: IconName; badge?: string; badgeTone?: 'muted' | 'brand' };

function MenuSection({
  title,
  items,
  colors,
  onPress,
}: {
  title: string;
  items: MenuItem[];
  colors: ReturnType<typeof useTheme>['colors'];
  onPress: (href: string) => void;
}) {
  return (
    <View style={styles.section}>
      <Text style={[styles.sectionTitle, { color: colors.text.muted }]}>{title}</Text>
      <Card style={styles.card}>
        {items.map((item, index) => (
          <TouchableOpacity
            key={item.label}
            style={[
              styles.menuRow,
              { borderBottomColor: colors.border },
              index === items.length - 1 && styles.menuRowLast,
            ]}
            onPress={() => onPress(item.href)}
            activeOpacity={0.7}
          >
            <View style={[styles.menuIconWrap, { backgroundColor: colors.bg.alt }]}>
              <Icon name={item.icon} size={18} color={colors.text.body} />
            </View>
            <Text style={[styles.menuLabel, { color: colors.text.primary }]}>{item.label}</Text>
            {item.badge ? (
              <View
                style={[
                  styles.menuBadge,
                  { backgroundColor: item.badgeTone === 'brand' ? colors.brandLight : colors.bg.alt },
                ]}
              >
                <Text
                  style={[
                    styles.menuBadgeText,
                    { color: item.badgeTone === 'brand' ? colors.brand : colors.text.muted },
                  ]}
                >
                  {item.badge}
                </Text>
              </View>
            ) : null}
            <Icon name="forward" size={18} color={colors.text.subtle} />
          </TouchableOpacity>
        ))}
      </Card>
    </View>
  );
}

export default function MoreScreen() {
  const { t } = useTranslation();
  const { colors } = useTheme();
  const { signOut, user } = useAuth();
  const router = useRouter();

  // Same cache key as the Profile screen, so this card is instant if Profile
  // has already loaded once, and still resolves on its own otherwise.
  const { data: profile } = useQuery({ queryKey: ['profile'], queryFn: getProfile });
  const { data: subscription } = useQuery({ queryKey: ['subscription'], queryFn: getSubscription });
  const currentPlan = subscription?.currentPlan ?? 'Free';
  const displayName =
    profile && (profile.firstName || profile.lastName)
      ? [profile.firstName, profile.lastName].filter(Boolean).join(' ')
      : user?.firstName && user?.lastName
        ? `${user.firstName} ${user.lastName}`
        : t('more.profile');
  const displayEmail = profile?.email ?? user?.email ?? '';
  const initial = (profile?.firstName || user?.firstName || displayEmail)?.[0]?.toUpperCase() ?? '?';

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

  const features: MenuItem[] = [
    { label: t('more.income'), href: '/(tabs)/income', icon: 'income' },
    { label: t('more.accounts'), href: '/(tabs)/accounts', icon: 'accounts' },
    { label: t('more.transactions'), href: '/(tabs)/transactions', icon: 'transactions' },
    { label: t('more.recurring'), href: '/(tabs)/recurring', icon: 'recurring' },
    { label: t('more.categories'), href: '/(tabs)/categories', icon: 'categories' },
    { label: t('more.monthlyReport'), href: '/(tabs)/reports', icon: 'report' },
    { label: t('more.notifications'), href: '/(tabs)/notifications', icon: 'notifications' },
    {
      label: t('more.subscription'),
      href: '/(tabs)/subscription',
      icon: 'subscription',
      badge: currentPlan === 'Free' ? t('more.upgradeBadge') : currentPlan,
      badgeTone: currentPlan === 'Free' ? 'brand' : 'muted',
    },
  ];

  const general: MenuItem[] = [
    { label: t('more.profile'), href: '/(tabs)/profile', icon: 'profile' },
    { label: t('more.feedback'), href: '/(tabs)/feedback', icon: 'feedback' },
    { label: t('more.privacyPolicy'), href: '/(tabs)/privacy', icon: 'privacy' },
  ];

  return (
    <ScrollView
      style={[styles.container, { backgroundColor: colors.bg.alt }]}
      contentContainerStyle={[styles.content, { paddingTop: 16, paddingBottom: 40 }]}
    >
      <TouchableOpacity
        onPress={() => router.push('/(tabs)/profile')}
        activeOpacity={0.75}
        accessibilityLabel={t('more.viewProfile', { name: displayName })}
      >
        <Card style={styles.profileCard}>
          <View style={[styles.avatar, { backgroundColor: colors.brand }]}>
            <Text style={[styles.avatarText, { color: colors.brandContrast }]}>{initial}</Text>
          </View>
          <View style={styles.profileBody}>
            <Text style={[styles.profileName, { color: colors.text.primary }]} numberOfLines={1}>
              {displayName}
            </Text>
            {displayEmail ? (
              <Text style={[styles.profileEmail, { color: colors.text.muted }]} numberOfLines={1}>
                {displayEmail}
              </Text>
            ) : null}
          </View>
          <Icon name="forward" size={18} color={colors.text.subtle} />
        </Card>
      </TouchableOpacity>

      <MenuSection title={t('more.features')} items={features} colors={colors} onPress={(href) => router.push(href as any)} />
      <MenuSection title={t('more.general')} items={general} colors={colors} onPress={(href) => router.push(href as any)} />

      <TouchableOpacity
        style={[styles.signOut, { borderColor: colors.danger }]}
        onPress={handleSignOut}
        activeOpacity={0.7}
      >
        <Icon name="signOut" size={18} color={colors.danger} style={{ marginRight: 8 }} />
        <Text style={[styles.signOutText, { color: colors.danger }]}>{t('more.signOut')}</Text>
      </TouchableOpacity>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 16 },
  profileCard: { flexDirection: 'row', alignItems: 'center', marginBottom: 20 },
  avatar: {
    width: 48,
    height: 48,
    borderRadius: 24,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 14,
  },
  avatarText: { fontSize: 19, fontWeight: '700' },
  profileBody: { flex: 1, minWidth: 0 },
  profileName: { fontSize: 17, fontWeight: '700', marginBottom: 2 },
  profileEmail: { fontSize: 13 },
  menuBadge: { paddingHorizontal: 9, paddingVertical: 4, borderRadius: 8, marginRight: 8 },
  menuBadgeText: { fontSize: 12, fontWeight: '700' },
  section: { marginBottom: 24 },
  sectionTitle: {
    fontSize: 12,
    fontWeight: '700',
    letterSpacing: 0.8,
    marginBottom: 8,
    marginLeft: 4,
  },
  card: { padding: 0, overflow: 'hidden' },
  menuRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 14,
    paddingHorizontal: 16,
    borderBottomWidth: 1,
  },
  menuRowLast: { borderBottomWidth: 0 },
  menuIconWrap: {
    width: 36,
    height: 36,
    borderRadius: 10,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  menuLabel: { flex: 1, fontSize: 16 },
  signOut: {
    marginTop: 8,
    paddingVertical: 14,
    borderRadius: 12,
    borderWidth: 1.5,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
  },
  signOutText: { fontSize: 16, fontWeight: '600' },
});
