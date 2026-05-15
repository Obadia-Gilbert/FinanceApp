import { useState } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity, Alert, Platform, ActivityIndicator } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useTheme } from '../../src/context/ThemeContext';
import { Card } from '../../src/components/Card';
import { Button } from '../../src/components/Button';
import { getSubscription } from '../../src/api/subscription';
import { useSubscriptionIap } from '../../src/iap/useSubscriptionIap';
import { getStoreProductId } from '../../src/iap/config';

export default function SubscriptionScreen() {
  const { t } = useTranslation();
  const { colors } = useTheme();
  const [expandedFaq, setExpandedFaq] = useState<number | null>(null);
  const { available, loading, products, error, subscribe, restore } = useSubscriptionIap();

  const { data: sub, isLoading: subLoading } = useQuery({
    queryKey: ['subscription'],
    queryFn: getSubscription,
  });

  const currentPlan = sub?.currentPlan ?? 'Free';
  const billing = sub?.billingSource ?? 'None';
  const expires = sub?.subscriptionExpiresAtUtc
    ? new Date(sub.subscriptionExpiresAtUtc).toLocaleString()
    : null;

  const proSku = getStoreProductId('pro');
  const proProduct = products.find((p) => p.productId === proSku);
  const proPrice = proProduct?.localizedPrice ?? '$9.99';

  const showError = (message: string) => {
    Alert.alert(t('subscription.errors.title'), message);
  };

  const onSubscribePress = async () => {
    if (!available) {
      showError(error ?? t('subscription.errors.storeUnavailable'));
      return;
    }
    try {
      await subscribe('pro');
    } catch (e) {
      showError(e instanceof Error ? e.message : t('subscription.errors.purchaseFailed'));
    }
  };

  const onRestorePress = async () => {
    if (!available) {
      showError(error ?? t('subscription.errors.storeUnavailable'));
      return;
    }
    try {
      await restore();
      Alert.alert(t('subscription.restoreSuccessTitle'), t('subscription.restoreSuccessBody'));
    } catch (e) {
      showError(e instanceof Error ? e.message : t('subscription.errors.restoreFailed'));
    }
  };

  const faqItems = [
    { q: t('subscription.faq.cancel.q'), a: t('subscription.faq.cancel.a') },
    { q: t('subscription.faq.how.q'), a: t('subscription.faq.how.a') },
    { q: t('subscription.faq.web.q'), a: t('subscription.faq.web.a') },
  ];

  const freeFeatures = t('subscription.features.free', { returnObjects: true }) as string[];
  const proFeatures = t('subscription.features.pro', { returnObjects: true }) as string[];
  const premiumFeatures = t('subscription.features.premium', { returnObjects: true }) as string[];

  return (
    <ScrollView style={[styles.container, { backgroundColor: colors.bg.alt }]} contentContainerStyle={styles.content}>
      <Card style={[styles.currentCard, { borderLeftWidth: 4, borderLeftColor: colors.brand }]}>
        <View style={styles.currentRow}>
          <View style={[styles.currentIconWrap, { backgroundColor: `${colors.brand}15` }]}>
            <Text style={{ fontSize: 20 }}>⭐</Text>
          </View>
          <View style={{ flex: 1 }}>
            <Text style={[styles.currentLabel, { color: colors.text.primary }]}>
              {t('subscription.currentPlan')}
            </Text>
            {subLoading ? (
              <ActivityIndicator color={colors.brand} style={{ alignSelf: 'flex-start', marginTop: 4 }} />
            ) : (
              <Text style={[styles.currentPlan, { color: colors.brand }]}>{currentPlan}</Text>
            )}
            <Text style={[styles.billingMeta, { color: colors.text.muted }]}>
              {t('subscription.billing', { source: billing })}
              {expires ? ` · ${t('subscription.renewsEnds', { date: expires })}` : ''}
            </Text>
          </View>
        </View>
        <TouchableOpacity onPress={onRestorePress} disabled={loading} style={styles.restoreLink}>
          <Text style={[styles.restoreText, { color: colors.brand }]}>
            {t('subscription.restorePurchases')}
          </Text>
        </TouchableOpacity>
      </Card>

      {!available && error ? (
        <Text style={[styles.iapHint, { color: colors.text.muted }]}>{error}</Text>
      ) : null}

      <Text style={[styles.chooseTitle, { color: colors.text.primary }]}>{t('subscription.choosePlan')}</Text>

      <Card style={styles.planCard}>
        <Text style={[styles.planName, { color: colors.text.primary }]}>{t('subscription.plans.free')}</Text>
        <View style={styles.priceRow}>
          <Text style={[styles.price, { color: colors.text.primary }]}>$0</Text>
          <Text style={[styles.pricePeriod, { color: colors.text.muted }]}>{t('subscription.forever')}</Text>
        </View>
        <Text style={[styles.planDesc, { color: colors.text.muted }]}>{t('subscription.freeDesc')}</Text>
        <View style={styles.featureList}>
          {Array.isArray(freeFeatures) &&
            freeFeatures.map((f, i) => (
              <View key={i} style={styles.featureRow}>
                <Text style={[styles.check, { color: colors.success }]}>✓</Text>
                <Text style={[styles.featureText, { color: colors.text.body }]}>{f}</Text>
              </View>
            ))}
        </View>
        {currentPlan === 'Free' && (
          <View style={[styles.activeBadge, { backgroundColor: `${colors.success}15` }]}>
            <Text style={[styles.activeBadgeText, { color: colors.success }]}>
              {t('subscription.activePlan')}
            </Text>
          </View>
        )}
      </Card>

      <Card style={[styles.planCard, { borderColor: colors.brand, borderWidth: 2 }]}>
        <View style={[styles.mostPopular, { backgroundColor: colors.brand }]}>
          <Text style={styles.mostPopularText}>{t('subscription.mostPopular')}</Text>
        </View>
        <Text style={[styles.planName, { color: colors.text.primary }]}>{t('subscription.plans.pro')}</Text>
        <View style={styles.priceRow}>
          <Text style={[styles.price, { color: colors.text.primary }]}>{proPrice}</Text>
          <Text style={[styles.pricePeriod, { color: colors.text.muted }]}>{t('subscription.perMonth')}</Text>
        </View>
        <Text style={[styles.planDesc, { color: colors.text.muted }]}>{t('subscription.proDesc')}</Text>
        <Button
          title={
            loading
              ? t('subscription.processing')
              : Platform.OS === 'ios'
                ? t('subscription.subscribeAppStore')
                : t('subscription.subscribeGooglePlay')
          }
          style={styles.upgradeBtn}
          onPress={onSubscribePress}
          disabled={loading}
        />
        <View style={styles.featureList}>
          {Array.isArray(proFeatures) &&
            proFeatures.map((f, i) => (
              <View key={i} style={styles.featureRow}>
                <Text style={[styles.check, { color: colors.brand }]}>✓</Text>
                <Text style={[styles.featureText, { color: colors.text.body }]}>{f}</Text>
              </View>
            ))}
        </View>
      </Card>

      <Card style={styles.planCard}>
        <Text style={[styles.planName, { color: colors.text.primary }]}>{t('subscription.plans.premium')}</Text>
        <View style={styles.priceRow}>
          <Text style={[styles.price, { color: colors.text.primary }]}>$24.99</Text>
          <Text style={[styles.pricePeriod, { color: colors.text.muted }]}>{t('subscription.perMonth')}</Text>
        </View>
        <Text style={[styles.planDesc, { color: colors.text.muted }]}>{t('subscription.premiumDesc')}</Text>
        <TouchableOpacity style={[styles.contactBtn, { backgroundColor: colors.bg.hover, borderColor: colors.border }]}>
          <Text style={[styles.contactBtnText, { color: colors.text.primary }]}>
            {t('subscription.contactSales')}
          </Text>
        </TouchableOpacity>
        <View style={styles.featureList}>
          {Array.isArray(premiumFeatures) &&
            premiumFeatures.map((f, i) => (
              <View key={i} style={styles.featureRow}>
                <Text style={[styles.check, { color: colors.brand }]}>✓</Text>
                <Text style={[styles.featureText, { color: colors.text.body }]}>{f}</Text>
              </View>
            ))}
        </View>
      </Card>

      <Text style={[styles.faqTitle, { color: colors.text.primary }]}>{t('subscription.faqTitle')}</Text>
      <Card style={styles.faqCard}>
        {faqItems.map((faq, i) => (
          <TouchableOpacity
            key={i}
            style={[styles.faqRow, i < faqItems.length - 1 && { borderBottomWidth: 1, borderBottomColor: colors.border }]}
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

      <Text style={[styles.footer, { color: colors.text.subtle }]}>
        {Platform.OS === 'ios' ? t('subscription.footerIos') : t('subscription.footerAndroid')}
      </Text>
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
  billingMeta: { fontSize: 12, marginTop: 4 },
  restoreLink: { marginTop: 12, alignSelf: 'flex-start' },
  restoreText: { fontSize: 14, fontWeight: '600' },
  iapHint: { fontSize: 13, marginBottom: 16, lineHeight: 18 },
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
