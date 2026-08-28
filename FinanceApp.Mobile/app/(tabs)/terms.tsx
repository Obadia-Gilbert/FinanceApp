import { View, Text, StyleSheet, ScrollView } from 'react-native';
import { useTheme } from '../../src/context/ThemeContext';
import { Icon, type IconName } from '../../src/components/Icon';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

export default function TermsScreen() {
  const { colors } = useTheme();
  const insets = useSafeAreaInsets();

  return (
    <ScrollView
      style={[styles.container, { backgroundColor: colors.bg.alt }]}
      contentContainerStyle={[styles.content, { paddingBottom: insets.bottom + 24 }]}
      showsVerticalScrollIndicator={true}
    >
      <Text style={[styles.updated, { color: colors.text.muted }]}>Last updated: March 1, 2026</Text>
      <Text style={[styles.title, { color: colors.text.primary }]}>Terms of Service</Text>
      <Text style={[styles.lead, { color: colors.text.muted }]}>
        These terms govern your use of FinanceApp. By creating an account or using the app, you agree to them.
      </Text>

      <Section title="01  Acceptance of terms" icon="check" colors={colors}>
        <Text style={[styles.body, { color: colors.text.body }]}>
          By creating an account, you confirm you are at least 18 years old (or the age of legal majority in your jurisdiction) and agree to these Terms of Service and our Privacy Policy. If you do not agree, do not use FinanceApp.
        </Text>
      </Section>

      <Section title="02  What FinanceApp is" icon="wallet" colors={colors}>
        <Text style={[styles.body, { color: colors.text.body }]}>
          FinanceApp is a personal finance tracker: you manually record expenses, income, accounts, and budgets. FinanceApp does not connect to your bank, does not move money on your behalf, and is not a bank, broker, or licensed financial advisor. Nothing in the app is investment, tax, or legal advice.
        </Text>
      </Section>

      <Section title="03  Your account" icon="profile" colors={colors}>
        <Bullet colors={colors} text="You are responsible for keeping your login credentials secure and for all activity under your account." />
        <Bullet colors={colors} text="You must provide accurate information when you register." />
        <Bullet colors={colors} text="You may delete your account at any time from Profile → Delete Account; see our Privacy Policy for what happens to your data." />
      </Section>

      <Section title="04  Acceptable use" icon="security" colors={colors}>
        <Text style={[styles.body, { color: colors.text.body }]}>You agree not to:</Text>
        <Bullet colors={colors} text="Use the app for any unlawful purpose, or to store or transmit unlawful content." />
        <Bullet colors={colors} text="Attempt to gain unauthorized access to any account, system, or network connected to FinanceApp." />
        <Bullet colors={colors} text="Reverse-engineer, decompile, or interfere with the app's normal operation." />
        <Bullet colors={colors} text="Use automated tools to scrape or abuse the service." />
      </Section>

      <Section title="05  Subscriptions & billing" icon="subscription" colors={colors}>
        <Bullet colors={colors} text="FinanceApp offers optional paid subscription tiers (Pro, Premium) that unlock additional features. The Free tier remains available at no cost." />
        <Bullet colors={colors} text="Subscriptions purchased in the app are billed by Apple (App Store) or Google (Google Play) under their respective terms, and auto-renew until cancelled." />
        <Bullet colors={colors} text="You can manage or cancel your subscription at any time in your App Store or Google Play account settings. Cancelling stops future renewals; it does not refund the current billing period unless required by store policy or law." />
        <Bullet colors={colors} text="Prices are shown in the app before purchase and may vary by region and currency." />
      </Section>

      <Section title="06  Your content" icon="report" colors={colors}>
        <Text style={[styles.body, { color: colors.text.body }]}>
          You retain ownership of the financial data, receipts, and documents you enter or upload. You grant FinanceApp a limited license to store and process that content solely to operate the app for you — see our Privacy Policy for details.
        </Text>
      </Section>

      <Section title="07  Disclaimers" icon="warning" colors={colors}>
        <Text style={[styles.body, { color: colors.text.body }]}>
          FinanceApp is provided &quot;as is&quot; without warranties of any kind, express or implied. We do not guarantee the app will be uninterrupted, error-free, or that calculations (budgets, currency conversion, reports) will be free of mistakes. You are responsible for verifying important financial figures independently.
        </Text>
      </Section>

      <Section title="08  Limitation of liability" icon="lock" colors={colors}>
        <Text style={[styles.body, { color: colors.text.body }]}>
          To the maximum extent permitted by law, FinanceApp and its operators are not liable for any indirect, incidental, or consequential damages arising from your use of the app, including financial decisions made based on data in the app.
        </Text>
      </Section>

      <Section title="09  Changes to these terms" icon="edit" colors={colors}>
        <Text style={[styles.body, { color: colors.text.body }]}>
          We may update these terms from time to time. When we do, the &quot;Last updated&quot; date above will change, and significant changes will be communicated through the app. Continued use after an update constitutes acceptance of the revised terms.
        </Text>
      </Section>

      <Section title="10  Termination" icon="close" colors={colors}>
        <Text style={[styles.body, { color: colors.text.body }]}>
          You may stop using FinanceApp and delete your account at any time. We may suspend or terminate access for accounts that violate these terms, including the acceptable-use rules in Section 4.
        </Text>
      </Section>

      <View style={[styles.contactCard, { backgroundColor: colors.bg.default, borderColor: colors.border }]}>
        <Text style={[styles.contactTitle, { color: colors.text.primary }]}>11  Contact Us</Text>
        <Text style={[styles.body, { color: colors.text.muted }]}>
          Questions about these terms? Contact support@financeapp.io.
        </Text>
      </View>
    </ScrollView>
  );
}

function Section({ title, icon, colors, children }: { title: string; icon: IconName; colors: any; children: React.ReactNode }) {
  return (
    <View style={styles.section}>
      <View style={styles.sectionHeader}>
        <View style={[styles.sectionIconWrap, { backgroundColor: colors.brandLight }]}>
          <Icon name={icon} size={16} color={colors.brand} />
        </View>
        <Text style={[styles.sectionTitle, { color: colors.brand }]}>{title}</Text>
      </View>
      {children}
    </View>
  );
}

function Bullet({ colors, text }: { colors: any; text: string }) {
  return <Text style={[styles.bullet, { color: colors.text.body }]}>• {text}</Text>;
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 16 },
  updated: { fontSize: 13, marginBottom: 8 },
  title: { fontSize: 22, fontWeight: '700', marginBottom: 8 },
  lead: { fontSize: 15, lineHeight: 22, marginBottom: 12 },
  body: { fontSize: 14, lineHeight: 22, marginBottom: 10 },
  section: { marginTop: 20, marginBottom: 8 },
  sectionHeader: { flexDirection: 'row', alignItems: 'center', marginBottom: 10 },
  sectionIconWrap: { width: 28, height: 28, borderRadius: 8, justifyContent: 'center', alignItems: 'center', marginRight: 10 },
  sectionTitle: { fontSize: 15, fontWeight: '700' },
  bullet: { fontSize: 14, lineHeight: 22, marginBottom: 4, marginLeft: 4 },
  contactCard: { marginTop: 24, padding: 16, borderRadius: 12, borderWidth: 1 },
  contactTitle: { fontSize: 15, fontWeight: '700', marginBottom: 8 },
});
