import { View, Text, StyleSheet, ScrollView, TouchableOpacity, Alert } from 'react-native';
import { useRouter } from 'expo-router';
import { useTheme } from '../../src/context/ThemeContext';
import { useAuth } from '../../src/context/AuthContext';
import { Card } from '../../src/components/Card';

type MenuItem = { label: string; href: string; icon: string };

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
            <Text style={styles.menuIcon}>{item.icon}</Text>
            <Text style={[styles.menuLabel, { color: colors.text.primary }]}>{item.label}</Text>
            <Text style={[styles.menuArrow, { color: colors.text.subtle }]}>›</Text>
          </TouchableOpacity>
        ))}
      </Card>
    </View>
  );
}

export default function MoreScreen() {
  const { colors } = useTheme();
  const { signOut } = useAuth();
  const router = useRouter();

  const handleSignOut = () => {
    Alert.alert('Sign out', 'Are you sure you want to sign out?', [
      { text: 'Cancel', style: 'cancel' },
      {
        text: 'Sign out',
        style: 'destructive',
        onPress: async () => {
          await signOut();
          router.replace('/(auth)/login');
        },
      },
    ]);
  };

  const features: MenuItem[] = [
    { label: 'Income', href: '/(tabs)/income', icon: '📥' },
    { label: 'Accounts', href: '/(tabs)/accounts', icon: '🏦' },
    { label: 'Transactions', href: '/(tabs)/transactions', icon: '↔' },
    { label: 'Recurring', href: '/(tabs)/recurring', icon: '🔄' },
    { label: 'Categories', href: '/(tabs)/categories', icon: '🏷' },
    { label: 'Monthly report', href: '/(tabs)/reports', icon: '📊' },
    { label: 'Notifications', href: '/(tabs)/notifications', icon: '🔔' },
    { label: 'Subscription', href: '/(tabs)/subscription', icon: '⭐' },
  ];

  const general: MenuItem[] = [
    { label: 'Profile', href: '/(tabs)/profile', icon: '👤' },
    { label: 'Feedback', href: '/(tabs)/feedback', icon: '💬' },
    { label: 'Privacy policy', href: '/(tabs)/privacy', icon: '🔒' },
  ];

  return (
    <ScrollView
      style={[styles.container, { backgroundColor: colors.bg.alt }]}
      contentContainerStyle={[styles.content, { paddingTop: 16, paddingBottom: 40 }]}
    >
      <MenuSection title="FEATURES" items={features} colors={colors} onPress={(href) => router.push(href as any)} />
      <MenuSection title="GENERAL" items={general} colors={colors} onPress={(href) => router.push(href as any)} />

      <TouchableOpacity
        style={[styles.signOut, { borderColor: colors.border }]}
        onPress={handleSignOut}
        activeOpacity={0.7}
      >
        <Text style={[styles.signOutText, { color: colors.danger }]}>Sign out</Text>
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
  menuIcon: { fontSize: 20, marginRight: 12 },
  menuLabel: { flex: 1, fontSize: 16 },
  menuArrow: { fontSize: 20 },
  signOut: {
    marginTop: 8,
    paddingVertical: 14,
    borderRadius: 8,
    borderWidth: 1,
    alignItems: 'center',
  },
  signOutText: { fontSize: 16, fontWeight: '600' },
});
