import Constants, { ExecutionEnvironment } from 'expo-constants';
import { Platform } from 'react-native';
import { getRedirectUrl, makeRedirectUri } from 'expo-auth-session';

/**
 * Expo Go must use an https redirect on the **Web application** OAuth client.
 * Google rejects custom schemes (com.googleusercontent.apps.…) and exp:// in that field.
 * @see https://docs.expo.dev/guides/google-authentication/ (native SDK needs a dev build)
 */
export function getExpoAuthProxyRedirectUri(): string {
  const slug = Constants.expoConfig?.slug ?? 'financeapp-mobile';
  const owner = Constants.expoConfig?.owner?.trim() || 'anonymous';
  return `https://auth.expo.io/@${owner}/${slug}`;
}

/** Reversed iOS client scheme — used by native SDK in dev builds, not in Google Web redirect URIs. */
export function googleReversedIosRedirectUri(iosClientId: string): string | null {
  const id = iosClientId.trim();
  if (!id.endsWith('.apps.googleusercontent.com')) return null;
  const prefix = id.slice(0, -'.apps.googleusercontent.com'.length);
  return `com.googleusercontent.apps.${prefix}:/oauthredirect`;
}

export function getGoogleOAuthRedirectUri(): string {
  if (Constants.executionEnvironment === ExecutionEnvironment.StoreClient) {
    try {
      return getRedirectUrl();
    } catch {
      return getExpoAuthProxyRedirectUri();
    }
  }
  if (Platform.OS === 'ios') {
    return makeRedirectUri({ scheme: 'financeapp', path: 'oauthredirect', preferLocalhost: true });
  }
  return makeRedirectUri({ scheme: 'financeapp', path: 'oauthredirect' });
}
