import { View, Text, StyleSheet, ScrollView, TouchableOpacity } from 'react-native';
import { router } from 'expo-router';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useTheme } from '../../src/context/ThemeContext';

export default function AuthTermsScreen() {
  const { colors } = useTheme();
  const insets = useSafeAreaInsets();
  return (
    <View style={styles.flex}>
      <TouchableOpacity style={[styles.backWrap, { paddingTop: insets.top + 16 }]} onPress={() => router.back()}>
        <Text style={[styles.backArrow, { color: colors.brand }]}>← Back</Text>
      </TouchableOpacity>
      <ScrollView
        style={[styles.container, { backgroundColor: colors.bg.alt }]}
        contentContainerStyle={[styles.content, { paddingBottom: insets.bottom + 24 }]}
      >
        <Text style={[styles.title, { color: colors.text.primary }]}>Terms of Service</Text>
        <Text style={[styles.updated, { color: colors.text.muted }]}>Last updated: March 1, 2026</Text>
        <Text style={[styles.body, { color: colors.text.body }]}>
          By creating a FinanceApp account you agree to these terms. FinanceApp is a manual personal finance tracker — it does not connect to your bank or move money on your behalf, and nothing in the app is financial, tax, or legal advice.
        </Text>
        <Text style={[styles.sectionTitle, { color: colors.brand }]}>Your Account</Text>
        <Text style={[styles.body, { color: colors.text.body }]}>
          You&apos;re responsible for keeping your login secure and for the accuracy of the information you provide. You can delete your account at any time from Profile → Delete Account.
        </Text>
        <Text style={[styles.sectionTitle, { color: colors.brand }]}>Subscriptions</Text>
        <Text style={[styles.body, { color: colors.text.body }]}>
          Paid tiers (Pro, Premium) are billed by Apple or Google and auto-renew until cancelled in your App Store or Google Play account settings. The Free tier remains available at no cost.
        </Text>
        <Text style={[styles.sectionTitle, { color: colors.brand }]}>No Warranty</Text>
        <Text style={[styles.body, { color: colors.text.body }]}>
          FinanceApp is provided &quot;as is.&quot; We don&apos;t guarantee it will be error-free, and you should independently verify important financial figures.
        </Text>
        <Text style={[styles.body, { color: colors.text.body }]}>
          The full terms are available in-app under More → Terms of Service once signed in.
        </Text>
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  backWrap: { paddingHorizontal: 16, paddingBottom: 8 },
  backArrow: { fontSize: 16, fontWeight: '600' },
  container: { flex: 1 },
  content: { padding: 16, paddingBottom: 40 },
  title: { fontSize: 22, fontWeight: '700', marginBottom: 8 },
  updated: { fontSize: 14, marginBottom: 20 },
  sectionTitle: { fontSize: 16, fontWeight: '700', marginTop: 16, marginBottom: 8 },
  body: { fontSize: 15, lineHeight: 24, marginBottom: 12 },
});
