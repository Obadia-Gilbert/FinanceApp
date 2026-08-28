import { useEffect, useState } from 'react';
import { View, Text, Image, StyleSheet } from 'react-native';
import { getApiBase, getStoredToken } from '../api/client';
import { useTheme } from '../context/ThemeContext';

/**
 * The signed-in user's profile photo, falling back to their initial.
 *
 * The photo can only be uploaded on the web app, which stores it as a
 * root-relative URL ("/uploads/profiles/x.jpg") that resolves against the MVC
 * origin — not the API's. Mobile therefore can't use that path directly and
 * instead reads GET /api/profile/image, which is authenticated, so the bearer
 * token has to travel with the image request.
 */
type Props = {
  /** Falsy when the user has never uploaded a photo — renders the initial. */
  hasImage: boolean;
  initial: string;
  size?: number;
  /** Bumped by the caller to bypass the cache after an upload. */
  cacheKey?: string | number;
};

export function ProfileAvatar({ hasImage, initial, size = 80, cacheKey }: Props) {
  const { colors } = useTheme();
  const [token, setToken] = useState<string | null>(null);
  // A stored path is no guarantee the file is still there, so fall back to the
  // initial rather than leaving a broken-image box behind. The failure is
  // recorded against the image's identity, so a fresh upload (or a switch back
  // to having no photo) retries rather than staying stuck on the fallback.
  const imageKey = `${hasImage}:${cacheKey ?? ''}`;
  const [failedKey, setFailedKey] = useState<string | null>(null);
  const failed = failedKey === imageKey;

  useEffect(() => {
    let active = true;
    getStoredToken().then((t) => {
      if (active) setToken(t);
    });
    return () => {
      active = false;
    };
  }, []);

  const shape = {
    width: size,
    height: size,
    borderRadius: size / 2,
  };

  if (hasImage && token && !failed) {
    return (
      <Image
        source={{
          uri: `${getApiBase()}/api/profile/image${cacheKey ? `?v=${cacheKey}` : ''}`,
          headers: { Authorization: `Bearer ${token}` },
        }}
        style={[shape, { backgroundColor: colors.bg.alt }]}
        onError={() => setFailedKey(imageKey)}
        accessibilityIgnoresInvertColors
      />
    );
  }

  return (
    <View style={[shape, styles.center, { backgroundColor: colors.brand }]}>
      <Text style={[styles.initial, { color: colors.brandContrast, fontSize: size * 0.4 }]}>
        {initial}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  center: { justifyContent: 'center', alignItems: 'center' },
  initial: { fontWeight: '700' },
});
