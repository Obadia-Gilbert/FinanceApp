import { View, Text, StyleSheet, ScrollView, TouchableOpacity, Alert } from 'react-native';
import { useRouter } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { useTheme } from '../../src/context/ThemeContext';
import { useAuth } from '../../src/context/AuthContext';
import { Card } from '../../src/components/Card';
import { Icon, type IconName } from '../../src/components/Icon';

type MenuItem = { label: string; href: string; icon: IconName };

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
  const { signOut } = useAuth();
  const router = useRouter();

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
    { label: t('more.subscription'), href: '/(tabs)/subscription', icon: 'subscription' },
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
