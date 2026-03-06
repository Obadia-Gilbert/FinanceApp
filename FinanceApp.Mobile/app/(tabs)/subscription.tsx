import { useState } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { useTheme } from '../../src/context/ThemeContext';
import { Card } from '../../src/components/Card';
import { Button } from '../../src/components/Button';
import { getSubscription } from '../../src/api/subscription';

const FREE_FEATURES = ['Basic expense tracking', 'Budget management', 'Monthly reports', 'Up to 3 accounts'];
const PRO_FEATURES = ['Unlimited Budgets', 'AI-Powered Insights', 'Receipt Scanning (OCR)', 'CSV & PDF Export', 'Custom Categories', 'Priority Support'];
const ENTERPRISE_FEATURES = ['All Pro Features', 'Multi-user Access', 'API Integration', 'Dedicated Support', 'Custom Branding'];
const FAQ_ITEMS = [
  { q: 'What happens to my data if I cancel?', a: 'Your data remains available for export. We retain it for 30 days after cancellation.' },
  { q: 'Can I switch plans anytime?', a: 'Yes. You can upgrade or downgrade your plan at any time from the web app or contact support.' },
  { q: 'Do you offer annual billing?', a: 'Annual billing is available with a 20% discount. Check the web app for details.' },
];

export default function SubscriptionScreen() {
  const { colors } = useTheme();
  const [expandedFaq, setExpandedFaq] = useState<number | null>(null);

  const { data: sub } = useQuery({
    queryKey: ['subscription'],
    queryFn: getSubscription,
  });

  const currentPlan = sub?.currentPlan ?? 'Free';

  return (
    <ScrollView style={[styles.container, { backgroundColor: colors.bg.alt }]} contentContainerStyle={styles.content}>
      {/* Current Plan */}
      <Card style={[styles.currentCard, { borderLeftWidth: 4, borderLeftColor: colors.brand }]}>
        <View style={styles.currentRow}>
          <View style={[styles.currentIconWrap, { backgroundColor: `${colors.brand}15` }]}>
            <Text style={{ fontSize: 20 }}>⭐</Text>
          </View>
          <View>
            <Text style={[styles.currentLabel, { color: colors.text.primary }]}>Current Plan</Text>
            <Text style={[styles.currentPlan, { color: colors.brand }]}>{currentPlan}</Text>
          </View>
        </View>
      </Card>

      <Text style={[styles.chooseTitle, { color: colors.text.primary }]}>Choose Your Plan</Text>

      {/* Free card */}
      <Card style={styles.planCard}>
        <Text style={[styles.planName, { color: colors.text.primary }]}>Free</Text>
        <View style={styles.priceRow}>
          <Text style={[styles.price, { color: colors.text.primary }]}>$0</Text>
          <Text style={[styles.pricePeriod, { color: colors.text.muted }]}>/ forever</Text>
        </View>
        <Text style={[styles.planDesc, { color: colors.text.muted }]}>
          Get started with essential finance tracking.
        </Text>
        <View style={styles.featureList}>
          {FREE_FEATURES.map((f, i) => (
            <View key={i} style={styles.featureRow}>
              <Text style={[styles.check, { color: colors.success }]}>✓</Text>
              <Text style={[styles.featureText, { color: colors.text.body }]}>{f}</Text>
            </View>
          ))}
        </View>
        {currentPlan === 'Free' && (
          <View style={[styles.activeBadge, { backgroundColor: `${colors.success}15` }]}>
            <Text style={[styles.activeBadgeText, { color: colors.success }]}>Active Plan</Text>
          </View>
        )}
      </Card>

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
          AI insights and advanced tracking for power users.
        </Text>
        <Button title="Upgrade to Pro" style={styles.upgradeBtn} onPress={() => {}} />
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
        <TouchableOpacity style={[styles.contactBtn, { backgroundColor: colors.bg.hover, borderColor: colors.border }]}>
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
      <Card style={styles.faqCard}>
        {FAQ_ITEMS.map((faq, i) => (
          <TouchableOpacity
            key={i}
            style={[styles.faqRow, i < FAQ_ITEMS.length - 1 && { borderBottomWidth: 1, borderBottomColor: colors.border }]}
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
      </Card>

      <Text style={[styles.footer, { color: colors.text.subtle }]}>SECURE CHECKOUT VIA APP STORE</Text>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 16, paddingBottom: 40 },
  currentCard: { marginBottom: 24 },
  currentRow: { flexDirection: 'row', alignItems: 'center', gap: 14 },
  currentIconWrap: { width: 44, height: 44, borderRadius: 12, justifyContent: 'center', alignItems: 'center' },
  currentLabel: { fontSize: 13, fontWeight: '500' },
  currentPlan: { fontSize: 20, fontWeight: '700' },
  chooseTitle: { fontSize: 20, fontWeight: '700', marginBottom: 16 },
  planCard: { marginBottom: 20, padding: 20, position: 'relative', overflow: 'visible' },
  mostPopular: { position: 'absolute', top: -12, right: 16, paddingHorizontal: 12, paddingVertical: 5, borderRadius: 8 },
  mostPopularText: { color: '#fff', fontSize: 11, fontWeight: '700', letterSpacing: 0.5 },
  planName: { fontSize: 20, fontWeight: '700', marginBottom: 8 },
  priceRow: { flexDirection: 'row', alignItems: 'baseline', marginBottom: 8 },
  price: { fontSize: 32, fontWeight: '800' },
  pricePeriod: { fontSize: 14, marginLeft: 4 },
  planDesc: { fontSize: 14, marginBottom: 16, lineHeight: 20 },
  upgradeBtn: { marginBottom: 16 },
  contactBtn: { paddingVertical: 12, borderRadius: 10, alignItems: 'center', marginBottom: 16, borderWidth: 1 },
  contactBtnText: { fontSize: 15, fontWeight: '600' },
  featureList: { gap: 10 },
  featureRow: { flexDirection: 'row', alignItems: 'center' },
  check: { fontSize: 16, marginRight: 10, fontWeight: '700' },
  featureText: { fontSize: 14 },
  activeBadge: { marginTop: 16, alignSelf: 'center', paddingHorizontal: 16, paddingVertical: 8, borderRadius: 20 },
  activeBadgeText: { fontSize: 14, fontWeight: '600' },
  faqTitle: { fontSize: 18, fontWeight: '700', marginTop: 8, marginBottom: 12 },
  faqCard: { padding: 0, overflow: 'hidden', marginBottom: 16 },
  faqRow: { paddingVertical: 14, paddingHorizontal: 16 },
  faqQRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  faqQ: { fontSize: 15, fontWeight: '500', flex: 1, marginRight: 8 },
  faqChevron: { fontSize: 12 },
  faqA: { fontSize: 14, marginTop: 10, lineHeight: 20 },
  footer: { fontSize: 11, textAlign: 'center', marginTop: 16, letterSpacing: 0.5 },
});
