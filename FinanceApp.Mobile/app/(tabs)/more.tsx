import { View, Text, StyleSheet, ScrollView, TouchableOpacity, Alert } from 'react-native';
import { useRouter } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { useTheme } from '../../src/context/ThemeContext';
import { useAuth } from '../../src/context/AuthContext';
import { Card } from '../../src/components/Card';

type MenuItem = { label: string; href: string; icon: string; iconBg: string };

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
            <View style={[styles.menuIconWrap, { backgroundColor: item.iconBg }]}>
              <Text style={styles.menuIcon}>{item.icon}</Text>
            </View>
            <Text style={[styles.menuLabel, { color: colors.text.primary }]}>{item.label}</Text>
            <Text style={[styles.menuArrow, { color: colors.text.subtle }]}>›</Text>
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
    { label: t('more.income'), href: '/(tabs)/income', icon: '📥', iconBg: `${colors.success}15` },
    { label: t('more.accounts'), href: '/(tabs)/accounts', icon: '🏦', iconBg: `${colors.brand}15` },
    { label: t('more.transactions'), href: '/(tabs)/transactions', icon: '↔', iconBg: `${colors.info}15` },
    { label: t('more.recurring'), href: '/(tabs)/recurring', icon: '🔄', iconBg: `${colors.warning}15` },
    { label: t('more.categories'), href: '/(tabs)/categories', icon: '🏷', iconBg: `${colors.brand}15` },
    { label: t('more.monthlyReport'), href: '/(tabs)/reports', icon: '📊', iconBg: `${colors.success}15` },
    { label: t('more.notifications'), href: '/(tabs)/notifications', icon: '🔔', iconBg: `${colors.danger}15` },
    { label: t('more.subscription'), href: '/(tabs)/subscription', icon: '⭐', iconBg: `${colors.warning}15` },
  ];

  const general: MenuItem[] = [
    { label: t('more.profile'), href: '/(tabs)/profile', icon: '👤', iconBg: `${colors.brand}15` },
    { label: t('more.feedback'), href: '/(tabs)/feedback', icon: '💬', iconBg: `${colors.info}15` },
    { label: t('more.privacyPolicy'), href: '/(tabs)/privacy', icon: '🔒', iconBg: `${colors.text.subtle}20` },
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
        <Text style={{ fontSize: 18, marginRight: 8 }}>🚪</Text>
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
  menuIcon: { fontSize: 18 },
  menuLabel: { flex: 1, fontSize: 16 },
  menuArrow: { fontSize: 20 },
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
