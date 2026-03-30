import { useState, useEffect, useRef } from 'react';
import { Redirect } from 'expo-router';
import { View, Image, Text, StyleSheet, Animated, Easing } from 'react-native';
import { useAuth } from '../src/context/AuthContext';
import { useTheme } from '../src/context/ThemeContext';

const SPLASH_MIN_MS = 2200;

export default function Index() {
  const { isReady, isSignedIn } = useAuth();
  const { colors, isDark } = useTheme();
  const [showSplash, setShowSplash] = useState(true);

  const logoScale = useRef(new Animated.Value(0.3)).current;
  const logoOpacity = useRef(new Animated.Value(0)).current;
  const textOpacity = useRef(new Animated.Value(0)).current;
  const dotScale = useRef(new Animated.Value(0)).current;
  const pulseAnim = useRef(new Animated.Value(1)).current;

  useEffect(() => {
    Animated.sequence([
      Animated.parallel([
        Animated.spring(logoScale, {
          toValue: 1,
          friction: 4,
          tension: 60,
          useNativeDriver: true,
        }),
        Animated.timing(logoOpacity, {
          toValue: 1,
          duration: 600,
          useNativeDriver: true,
        }),
      ]),
      Animated.parallel([
        Animated.timing(textOpacity, {
          toValue: 1,
          duration: 500,
          useNativeDriver: true,
        }),
        Animated.spring(dotScale, {
          toValue: 1,
          friction: 3,
          tension: 80,
          useNativeDriver: true,
        }),
      ]),
    ]).start();

    const timer = setTimeout(() => setShowSplash(false), SPLASH_MIN_MS);
    return () => clearTimeout(timer);
  }, []);

  useEffect(() => {
    const loop = Animated.loop(
      Animated.sequence([
        Animated.timing(pulseAnim, { toValue: 0.4, duration: 800, easing: Easing.inOut(Easing.ease), useNativeDriver: true }),
        Animated.timing(pulseAnim, { toValue: 1, duration: 800, easing: Easing.inOut(Easing.ease), useNativeDriver: true }),
      ])
    );
    loop.start();
    return () => loop.stop();
  }, []);

  if (showSplash || !isReady) {
    return (
      <View style={[styles.splash, { backgroundColor: isDark ? '#0F172A' : '#FFFFFF' }]}>
        <Animated.View style={[styles.logoWrap, { transform: [{ scale: logoScale }], opacity: logoOpacity }]}>
          <Image
            source={require('../assets/logo.png')}
            style={styles.logo}
            resizeMode="contain"
          />
        </Animated.View>

        <Animated.View style={{ opacity: textOpacity }}>
          <Text style={[styles.appName, { color: colors.brand }]}>FinanceApp</Text>
          <Text style={[styles.tagline, { color: colors.text.muted }]}>
            Track. Budget. Thrive.
          </Text>
        </Animated.View>

        <Animated.View style={[styles.loaderRow, { opacity: pulseAnim }]}>
          {[0, 1, 2].map((i) => (
            <Animated.View
              key={i}
              style={[
                styles.dot,
                { backgroundColor: colors.brand, transform: [{ scale: dotScale }] },
              ]}
            />
          ))}
        </Animated.View>
      </View>
    );
  }

  if (isSignedIn) {
    return <Redirect href="/(tabs)" />;
  }

  return <Redirect href="/(auth)/login" />;
}

const styles = StyleSheet.create({
  splash: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingBottom: 60,
  },
  logoWrap: {
    width: 120,
    height: 120,
    borderRadius: 28,
    overflow: 'hidden',
    marginBottom: 24,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.15,
    shadowRadius: 12,
    elevation: 8,
  },
  logo: {
    width: 120,
    height: 120,
  },
  appName: {
    fontSize: 30,
    fontWeight: '800',
    textAlign: 'center',
    letterSpacing: 0.5,
  },
  tagline: {
    fontSize: 15,
    textAlign: 'center',
    marginTop: 6,
    letterSpacing: 0.3,
  },
  loaderRow: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    marginTop: 40,
    gap: 10,
  },
  dot: {
    width: 10,
    height: 10,
    borderRadius: 5,
  },
});
