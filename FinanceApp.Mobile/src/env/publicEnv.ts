import Constants from 'expo-constants';

/** Keys mirrored in `app.config.js` → `expo.extra` for runtime reads when Metro inlined an empty value. */
export type PublicEnvKey =
  | 'EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID'
  | 'EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID'
  | 'EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID'
  | 'EXPO_PUBLIC_FACEBOOK_APP_ID';

/**
 * Read `EXPO_PUBLIC_*` from the bundle (inlined at transform time) or from `expo.extra`
 * (set when Metro loads `app.config.js` + `.env`). The extra fallback fixes stale bundles
 * after `.env` is restored without a full rebundle.
 */
export function publicEnv(key: PublicEnvKey): string {
  const inlined = process.env[key]?.trim();
  if (inlined) return inlined;
  const extra = Constants.expoConfig?.extra as Record<string, unknown> | undefined;
  const fromExtra = extra?.[key];
  return typeof fromExtra === 'string' ? fromExtra.trim() : '';
}
