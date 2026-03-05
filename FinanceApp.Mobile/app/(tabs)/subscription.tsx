import { useState } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { useTheme } from '../../src/context/ThemeContext';
import { Card } from '../../src/components/Card';
import { Button } from '../../src/components/Button';
import { getSubscription } from '../../src/api/subscription';

const PRO_FEATURES = ['Unlimited Budgets', 'Receipt Scanning', 'CSV & PDF Export', 'Custom Categories'];
const ENTERPRISE_FEATURES = ['All Pro Features', 'Multi-user Access', 'API Integration', 'Dedicated Support'];
const FAQ_ITEMS = [
  { q: 'What happens to my data if I cancel?', a: 'Your data remains available for export. We retain it for 30 days after cancellation.' },
  { q: 'Can I switch plans anytime?', a: 'Yes. You can upgrade or downgrade your plan from the web app or contact support.' },
  { q: 'Do you offer annual billing?', a: 'Annual billing may be available with a discount. Check the web app or contact sales.' },
];

export default function SubscriptionScreen() {
  const { colors } = useTheme();
  const [expandedFaq, setExpandedFaq] = useState<number | null>(null);

  const { data: sub, isLoading } = useQuery({
    queryKey: ['subscription'],
    queryFn: getSubscription,
  });

  const currentPlan = sub?.currentPlan ?? 'Free';

  return (
    <ScrollView style={[styles.container, { backgroundColor: colors.bg.alt }]} contentContainerStyle={styles.content}>
      {/* Current Plan */}
      <Card style={styles.currentCard}>
        <View style={styles.currentRow}>
          <Text style={styles.currentIcon}>📄</Text>
          <Text style={[styles.currentLabel, { color: colors.text.primary }]}>Current Plan: {currentPlan}</Text>
        </View>
        <Text style={[styles.currentDesc, { color: colors.text.muted }]}>
          Upgrade to unlock advanced financial insights and unlimited account tracking.
        </Text>
        <TouchableOpacity style={[styles.viewBenefitsBtn, { backgroundColor: colors.brandLight ?? '#EFF6FF' }]}>
          <Text style={[styles.viewBenefitsText, { color: colors.brand }]}>View Benefits</Text>
        </TouchableOpacity>
      </Card>

      {/* Choose Your Plan */}
      <Text style={[styles.chooseTitle, { color: colors.text.primary }]}>Choose Your Plan</Text>
      <Text style={[styles.chooseSub, { color: colors.text.muted }]}>
        Select the best option for your financial goals.
      </Text>

      {/* Pro card */}
      <Card style={[styles.planCard, { borderColor: colors.brand, borderWidth: 2 }]}>
        <View style={[styles.mostPopular, { backgroundColor: colors.brand }]}>
          <Text style={styles.mostPopularText}>MOST POPULAR</Text>
        </View>
        <Text style={[styles.planName, { color: colors.text.primary }]}>Pro</Text>
        <View style={styles.priceRow}>
          <Text style={[styles.price, { color: colors.text.primary }]}>$9.99</Text>
          <Text style={[styles.pricePeriod, { color: colors.text.muted }]}>/ month</Text>
        </View>
        <Text style={[styles.planDesc, { color: colors.text.muted }]}>
          Best for individuals tracking daily spending.
        </Text>
        <Button title="Upgrade Now" style={styles.upgradeBtn} onPress={() => {}} />
        <View style={styles.featureList}>
          {PRO_FEATURES.map((f, i) => (
            <View key={i} style={styles.featureRow}>
              <Text style={[styles.check, { color: colors.brand }]}>✓</Text>
              <Text style={[styles.featureText, { color: colors.text.body }]}>{f}</Text>
            </View>
          ))}
        </View>
      </Card>

      {/* Enterprise card */}
      <Card style={styles.planCard}>
        <Text style={[styles.planName, { color: colors.text.primary }]}>Enterprise</Text>
        <View style={styles.priceRow}>
          <Text style={[styles.price, { color: colors.text.primary }]}>$24.99</Text>
          <Text style={[styles.pricePeriod, { color: colors.text.muted }]}>/ month</Text>
        </View>
        <Text style={[styles.planDesc, { color: colors.text.muted }]}>
          For teams and complex portfolio management.
        </Text>
        <TouchableOpacity style={[styles.contactBtn, { backgroundColor: colors.bg.hover }]}>
          <Text style={[styles.contactBtnText, { color: colors.text.primary }]}>Contact Sales</Text>
        </TouchableOpacity>
        <View style={styles.featureList}>
          {ENTERPRISE_FEATURES.map((f, i) => (
            <View key={i} style={styles.featureRow}>
              <Text style={[styles.check, { color: colors.brand }]}>✓</Text>
              <Text style={[styles.featureText, { color: colors.text.body }]}>{f}</Text>
            </View>
          ))}
        </View>
      </Card>

      {/* FAQ */}
      <Text style={[styles.faqTitle, { color: colors.text.primary }]}>Frequently Asked Questions</Text>
      {FAQ_ITEMS.map((faq, i) => (
        <TouchableOpacity
          key={i}
          style={[styles.faqRow, { borderBottomColor: colors.border }]}
          onPress={() => setExpandedFaq(expandedFaq === i ? null : i)}
          activeOpacity={0.7}
        >
          <View style={styles.faqQRow}>
            <Text style={[styles.faqQ, { color: colors.text.primary }]}>{faq.q}</Text>
            <Text style={[styles.faqChevron, { color: colors.text.muted }]}>{expandedFaq === i ? '▲' : '▼'}</Text>
          </View>
          {expandedFaq === i && (
            <Text style={[styles.faqA, { color: colors.text.body }]}>{faq.a}</Text>
          )}
        </TouchableOpacity>
      ))}

      <Text style={[styles.footer, { color: colors.text.muted }]}>SECURE CHECKOUT VIA APP STORE</Text>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 16, paddingBottom: 40 },
  currentCard: { marginBottom: 24 },
  currentRow: { flexDirection: 'row', alignItems: 'center', marginBottom: 8 },
  currentIcon: { fontSize: 20, marginRight: 8 },
  currentLabel: { fontSize: 16, fontWeight: '700' },
  currentDesc: { fontSize: 14, marginBottom: 16 },
  viewBenefitsBtn: { alignSelf: 'flex-start', paddingHorizontal: 16, paddingVertical: 10, borderRadius: 10 },
  viewBenefitsText: { fontSize: 14, fontWeight: '600' },
  chooseTitle: { fontSize: 20, fontWeight: '700', marginBottom: 8 },
  chooseSub: { fontSize: 14, marginBottom: 20 },
  planCard: { marginBottom: 20, padding: 20, position: 'relative', overflow: 'visible' },
  mostPopular: { position: 'absolute', top: -10, right: 16, paddingHorizontal: 12, paddingVertical: 4, borderRadius: 8 },
  mostPopularText: { color: '#fff', fontSize: 11, fontWeight: '700' },
  planName: { fontSize: 20, fontWeight: '700', marginBottom: 8 },
  priceRow: { flexDirection: 'row', alignItems: 'baseline', marginBottom: 8 },
  price: { fontSize: 28, fontWeight: '700' },
  pricePeriod: { fontSize: 14, marginLeft: 4 },
  planDesc: { fontSize: 14, marginBottom: 16 },
  upgradeBtn: { marginBottom: 16 },
  contactBtn: { paddingVertical: 12, borderRadius: 10, alignItems: 'center', marginBottom: 16 },
  contactBtnText: { fontSize: 15, fontWeight: '600' },
  featureList: { gap: 8 },
  featureRow: { flexDirection: 'row', alignItems: 'center' },
  check: { fontSize: 16, marginRight: 8 },
  featureText: { fontSize: 14 },
  faqTitle: { fontSize: 18, fontWeight: '700', marginTop: 8, marginBottom: 16 },
  faqRow: { paddingVertical: 14, borderBottomWidth: 1 },
  faqQRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  faqQ: { fontSize: 15, fontWeight: '500', flex: 1, marginRight: 8 },
  faqChevron: { fontSize: 12 },
  faqA: { fontSize: 14, marginTop: 12, lineHeight: 20 },
  footer: { fontSize: 11, textAlign: 'center', marginTop: 32, letterSpacing: 0.5 },
});
