import { View, Text, StyleSheet, ScrollView } from 'react-native';
import { useTheme } from '../../src/context/ThemeContext';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

export default function PrivacyScreen() {
  const { colors } = useTheme();
  const insets = useSafeAreaInsets();

  return (
    <ScrollView
      style={[styles.container, { backgroundColor: colors.bg.alt }]}
      contentContainerStyle={[styles.content, { paddingBottom: insets.bottom + 24 }]}
      showsVerticalScrollIndicator={true}
    >
      <Text style={[styles.updated, { color: colors.text.muted }]}>Last updated: March 1, 2026</Text>
      <Text style={[styles.title, { color: colors.text.primary }]}>Privacy Policy</Text>
      <Text style={[styles.lead, { color: colors.text.muted }]}>
        We believe your financial data belongs to you. This policy explains clearly what we collect, how we use it, and the controls you have over it.
      </Text>
      <Text style={[styles.body, { color: colors.text.body }]}>
        By using FinanceApp you agree to the practices described in this policy. If you do not agree, please discontinue use of the application.
      </Text>
      <View style={[styles.highlight, { backgroundColor: colors.brandLight ?? '#EFF6FF', borderLeftColor: colors.brand }]}>
        <Text style={[styles.highlightText, { color: colors.brand }]}>
          Short version: We collect only what we need to run the app. We never sell your data. You control your data.
        </Text>
      </View>

      <Section title="01  Overview" icon="🛡" colors={colors}>
        <Text style={[styles.body, { color: colors.text.body }]}>
          FinanceApp is a personal finance management application. We are committed to protecting your privacy and handling your data with care, transparency, and respect. This policy applies to all users of FinanceApp and describes how we collect, use, and safeguard your information when you use our service.
        </Text>
      </Section>

      <Section title="02  Information We Collect" icon="📊" colors={colors}>
        <Text style={[styles.body, { color: colors.text.body }]}>We collect the minimum information necessary to provide the service:</Text>
        <Text style={[styles.subhead, { color: colors.text.primary }]}>Account information</Text>
        <Bullet colors={colors} text="Name, email address, and password (stored as a secure hash — we never store plain-text passwords)" />
        <Bullet colors={colors} text="Optional profile photo you upload" />
        <Bullet colors={colors} text="Subscription plan and assignment date" />
        <Text style={[styles.subhead, { color: colors.text.primary }]}>Financial data you enter</Text>
        <Bullet colors={colors} text="Expense records: description, amount, currency, date, category" />
        <Bullet colors={colors} text="Account records: name, type, currency, balance" />
        <Bullet colors={colors} text="Transaction records: type, amount, date, category, notes" />
        <Bullet colors={colors} text="Budget settings and category configurations" />
        <Bullet colors={colors} text="Supporting documents and receipt files you attach" />
        <Text style={[styles.subhead, { color: colors.text.primary }]}>Technical data</Text>
        <Bullet colors={colors} text="Authentication tokens and session identifiers" />
        <Bullet colors={colors} text="Anti-forgery tokens for security" />
      </Section>

      <Section title="03  How We Use Your Data" icon="⚙" colors={colors}>
        <Text style={[styles.body, { color: colors.text.body }]}>Your data is used exclusively to operate and improve FinanceApp. Specifically, we use it to:</Text>
        <Bullet colors={colors} text="Authenticate you and maintain a secure session" />
        <Bullet colors={colors} text="Display your financial records, summaries, and budgets" />
        <Bullet colors={colors} text="Generate Excel exports of your data on demand" />
        <Bullet colors={colors} text="Serve uploaded receipt and document files back to you" />
        <Bullet colors={colors} text="Send security-related emails (e.g. password reset) when you request them" />
        <Bullet colors={colors} text="Improve application performance and fix bugs" />
        <Text style={[styles.body, { color: colors.text.body }]}>We do not use your financial data for advertising, profiling, or selling to third parties.</Text>
      </Section>

      <Section title="04  Storage & Security" icon="🔒" colors={colors}>
        <Text style={[styles.body, { color: colors.text.body }]}>We take security seriously and have implemented multiple layers of protection:</Text>
        <Bullet colors={colors} text="Passwords are hashed using industry-standard algorithms — the original password is never stored" />
        <Bullet colors={colors} text="Authentication uses secure tokens with validation on every state-changing request" />
        <Bullet colors={colors} text="Database is isolated per deployment — your data is not commingled without access controls" />
        <Bullet colors={colors} text="Uploaded files are stored server-side and accessed only by the authenticated owner" />
        <Bullet colors={colors} text="HTTPS is enforced in production to encrypt all data in transit" />
        <Bullet colors={colors} text="Soft deletes are used — when you delete a record it is flagged and excluded from queries" />
      </Section>

      <Section title="05  Data Retention" icon="🕐" colors={colors}>
        <Text style={[styles.body, { color: colors.text.body }]}>Your data is retained for as long as your account remains active. When you delete your account:</Text>
        <Bullet colors={colors} text="All financial records associated with your account are permanently removed" />
        <Bullet colors={colors} text="Uploaded files and receipts are deleted from the server" />
        <Bullet colors={colors} text="Account credentials are purged from the identity store" />
        <Text style={[styles.body, { color: colors.text.body }]}>You can request deletion of specific records at any time from within the application using the delete actions on each record.</Text>
      </Section>

      <Section title="06  Your Rights & Controls" icon="✓" colors={colors}>
        <Text style={[styles.body, { color: colors.text.body }]}>You have full control over your data within FinanceApp:</Text>
        <Bullet colors={colors} text="Access — view all your financial records, account details, and uploaded documents at any time" />
        <Bullet colors={colors} text="Correction — edit any record through the application's edit functions" />
        <Bullet colors={colors} text="Deletion — delete any record, document, or your entire account" />
        <Bullet colors={colors} text="Export — download your expense data as an Excel file at any time via the Export button on the Expenses page" />
        <Bullet colors={colors} text="Profile management — update your name, email, and profile photo from the Edit Profile page" />
        <Text style={[styles.body, { color: colors.text.body }]}>If you need assistance exercising any of these rights, please contact us using the information in Section 10.</Text>
      </Section>

      <Section title="07  Third-Party Services" icon="↗" colors={colors}>
        <Text style={[styles.body, { color: colors.text.body }]}>FinanceApp may load third-party resources to power the interface (e.g. fonts, icons). These services receive only your IP address and browser headers as part of standard HTTP requests. No financial data is transmitted to them. We do not use analytics tools, advertising networks, or tracking pixels.</Text>
      </Section>

      <Section title="08  Cookies" icon="🍪" colors={colors}>
        <Text style={[styles.body, { color: colors.text.body }]}>FinanceApp uses a minimal set of cookies strictly necessary for the application: session cookies for authentication, anti-forgery tokens for security, and local storage for theme preference. We do not use advertising or tracking cookies.</Text>
      </Section>

      <Section title="09  Changes to This Policy" icon="✎" colors={colors}>
        <Text style={[styles.body, { color: colors.text.body }]}>We may update this policy from time to time. When we do, the "Last updated" date at the top will be revised and significant changes will be communicated through the application. Continued use after an update constitutes acceptance of the revised policy.</Text>
      </Section>

      <View style={[styles.contactCard, { backgroundColor: colors.bg.default, borderColor: colors.border }]}>
        <Text style={[styles.contactTitle, { color: colors.text.primary }]}>10  Contact Us</Text>
        <Text style={[styles.body, { color: colors.text.muted }]}>
          Have questions, concerns, or a data request? We're here to help. Contact privacy@financeapp.local. We aim to respond to all privacy-related inquiries within 5 business days.
        </Text>
      </View>
    </ScrollView>
  );
}

function Section({ title, icon, colors, children }: { title: string; icon: string; colors: any; children: React.ReactNode }) {
  return (
    <View style={styles.section}>
      <View style={styles.sectionHeader}>
        <Text style={styles.sectionIcon}>{icon}</Text>
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
  highlight: { borderLeftWidth: 3, padding: 12, marginVertical: 12 },
  highlightText: { fontSize: 14, fontWeight: '600' },
  section: { marginTop: 20, marginBottom: 8 },
  sectionHeader: { flexDirection: 'row', alignItems: 'center', marginBottom: 10 },
  sectionIcon: { fontSize: 18, marginRight: 8 },
  sectionTitle: { fontSize: 15, fontWeight: '700' },
  subhead: { fontSize: 14, fontWeight: '600', marginTop: 12, marginBottom: 4 },
  bullet: { fontSize: 14, lineHeight: 22, marginBottom: 4, marginLeft: 4 },
  contactCard: { marginTop: 24, padding: 16, borderRadius: 12, borderWidth: 1 },
  contactTitle: { fontSize: 15, fontWeight: '700', marginBottom: 8 },
});
