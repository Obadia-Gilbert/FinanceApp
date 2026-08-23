import { useRef, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Dimensions,
  type NativeSyntheticEvent,
  type NativeScrollEvent,
} from 'react-native';
import { useRouter } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useTheme } from '../src/context/ThemeContext';
import { Icon, type IconName } from '../src/components/Icon';
import { Button } from '../src/components/Button';
import { markOnboardingComplete } from '../src/utils/onboarding';

const { width: SCREEN_WIDTH } = Dimensions.get('window');

const SLIDES: { icon: IconName; titleKey: string; bodyKey: string }[] = [
  { icon: 'wallet', titleKey: 'onboarding.slide1Title', bodyKey: 'onboarding.slide1Body' },
  { icon: 'budget', titleKey: 'onboarding.slide2Title', bodyKey: 'onboarding.slide2Body' },
  { icon: 'report', titleKey: 'onboarding.slide3Title', bodyKey: 'onboarding.slide3Body' },
];

export default function OnboardingScreen() {
  const { t } = useTranslation();
  const { colors } = useTheme();
  const insets = useSafeAreaInsets();
  const router = useRouter();
  const scrollRef = useRef<ScrollView>(null);
  const [page, setPage] = useState(0);

  const finish = () => {
    void (async () => {
      await markOnboardingComplete();
      router.replace('/(auth)/login');
    })();
  };

  const handleScrollEnd = (e: NativeSyntheticEvent<NativeScrollEvent>) => {
    const next = Math.round(e.nativeEvent.contentOffset.x / SCREEN_WIDTH);
    setPage(next);
  };

  const handleNext = () => {
    if (page === SLIDES.length - 1) {
      finish();
      return;
    }
    scrollRef.current?.scrollTo({ x: SCREEN_WIDTH * (page + 1), animated: true });
    setPage(page + 1);
  };

  const isLast = page === SLIDES.length - 1;

  return (
    <View style={[styles.container, { backgroundColor: colors.bg.default, paddingTop: insets.top }]}>
      <View style={styles.topRow}>
        <TouchableOpacity onPress={finish} hitSlop={12}>
          <Text style={[styles.skip, { color: colors.text.muted }]}>{t('onboarding.skip')}</Text>
        </TouchableOpacity>
      </View>

      <ScrollView
        ref={scrollRef}
        horizontal
        pagingEnabled
        showsHorizontalScrollIndicator={false}
        onMomentumScrollEnd={handleScrollEnd}
        scrollEventThrottle={16}
      >
        {SLIDES.map((slide) => (
          <View key={slide.titleKey} style={[styles.slide, { width: SCREEN_WIDTH }]}>
            <View style={[styles.iconWrap, { backgroundColor: colors.brandLight ?? `${colors.brand}15` }]}>
              <Icon name={slide.icon} size={48} color={colors.brand} />
            </View>
            <Text style={[styles.title, { color: colors.text.primary }]}>{t(slide.titleKey)}</Text>
            <Text style={[styles.body, { color: colors.text.muted }]}>{t(slide.bodyKey)}</Text>
          </View>
        ))}
      </ScrollView>

      <View style={styles.footer}>
        <View style={styles.dotsRow}>
          {SLIDES.map((slide, i) => (
            <View
              key={slide.titleKey}
              style={[
                styles.dot,
                {
                  backgroundColor: i === page ? colors.brand : colors.border,
                  width: i === page ? 20 : 8,
                },
              ]}
            />
          ))}
        </View>
        <Button
          title={isLast ? t('onboarding.getStarted') : t('onboarding.next')}
          onPress={handleNext}
          style={styles.cta}
        />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  topRow: { flexDirection: 'row', justifyContent: 'flex-end', paddingHorizontal: 20, paddingTop: 8 },
  skip: { fontSize: 15, fontWeight: '600' },
  slide: { alignItems: 'center', justifyContent: 'center', paddingHorizontal: 32 },
  iconWrap: {
    width: 96,
    height: 96,
    borderRadius: 48,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 28,
  },
  title: { fontSize: 24, fontWeight: '800', textAlign: 'center', marginBottom: 12 },
  body: { fontSize: 15, lineHeight: 22, textAlign: 'center' },
  footer: { paddingHorizontal: 24, paddingBottom: 32, paddingTop: 12 },
  dotsRow: { flexDirection: 'row', justifyContent: 'center', alignItems: 'center', gap: 8, marginBottom: 24 },
  dot: { height: 8, borderRadius: 4 },
  cta: { width: '100%' },
});
