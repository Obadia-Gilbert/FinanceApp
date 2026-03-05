import { View, Text, StyleSheet, ScrollView, TouchableOpacity } from 'react-native';
import { router } from 'expo-router';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useTheme } from '../../src/context/ThemeContext';

export default function AuthPrivacyScreen() {
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
        <Text style={[styles.title, { color: colors.text.primary }]}>Privacy Policy</Text>
        <Text style={[styles.updated, { color: colors.text.muted }]}>Last updated: March 2026</Text>
        <Text style={[styles.body, { color: colors.text.body }]}>
          At FinanceApp, we take your privacy seriously. This policy describes how we collect, use, and handle your information when you use our personal finance management platform.
        </Text>
        <Text style={[styles.sectionTitle, { color: colors.brand }]}>Data Collection</Text>
        <Text style={[styles.body, { color: colors.text.body }]}>
          To provide our financial tracking services, we collect information that you provide directly to us: account information (name, email, encrypted credentials), financial data (transactions, balances, budget categories), and usage data.
        </Text>
        <Text style={[styles.sectionTitle, { color: colors.brand }]}>Your Rights</Text>
        <Text style={[styles.body, { color: colors.text.body }]}>
          You have the right to access, correct, or delete your personal information at any time. You can export your financial data from the app.
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
