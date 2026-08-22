import { Platform } from 'react-native';
import { makeRedirectUri } from 'expo-auth-session';
import type { GoogleAuthRequestConfig } from 'expo-auth-session/providers/google';
import { publicEnv } from '../env/publicEnv';
import { getExpoAuthProxyRedirectUri, googleReversedIosRedirectUri } from './googleRedirectUri';

function optionalEnv(key: Parameters<typeof publicEnv>[0]): string | undefined {
  const v = publicEnv(key);
  return v ? v : undefined;
}

/**
 * OAuth config for Expo Go browser sign-in.
 * iOS must pair the **iOS** client ID with the reversed `com.googleusercontent.apps.*` redirect —
 * not `auth.expo.io` (Web client only, proxy deprecated).
 */
export function buildGoogleExpoGoAuthConfig(): {
  config: Partial<GoogleAuthRequestConfig>;
  redirectUri: string;
} {
  const webClientId = optionalEnv('EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID');
  const iosClientId = optionalEnv('EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID');
  const androidClientId = optionalEnv('EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID');

  if (Platform.OS === 'ios' && iosClientId) {
    const redirectUri = googleReversedIosRedirectUri(iosClientId);
    if (redirectUri) {
      return {
        redirectUri,
        config: {
          webClientId,
          iosClientId,
          clientId: iosClientId,
          redirectUri,
        },
      };
    }
  }

  if (Platform.OS === 'android' && androidClientId) {
    const redirectUri = makeRedirectUri({ scheme: 'financeapp', path: 'oauthredirect' });
    return {
      redirectUri,
      config: {
        webClientId,
        androidClientId,
        clientId: androidClientId,
        redirectUri,
      },
    };
  }

  const redirectUri = getExpoAuthProxyRedirectUri();
  return {
    redirectUri,
    config: {
      webClientId,
      clientId: webClientId,
      redirectUri,
    },
  };
}
