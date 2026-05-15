/**
 * True when `EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID` is set — enough for Expo Go (browser OAuth).
 * Development builds with the native SDK on iOS should also set `EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID`
 * (validated in `GoogleSignInButtonImpl` on sign-in).
 */
export function isGoogleAuthConfigured(): boolean {
  return Boolean(process.env.EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID?.trim());
}

export function isFacebookAuthConfigured(): boolean {
  return Boolean(process.env.EXPO_PUBLIC_FACEBOOK_APP_ID?.trim());
}

export function facebookAppId(): string {
  return process.env.EXPO_PUBLIC_FACEBOOK_APP_ID?.trim() ?? '';
}
