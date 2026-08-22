import { useEffect, useMemo, useState } from 'react';
import { Text, TouchableOpacity, ActivityIndicator } from 'react-native';
import * as WebBrowser from 'expo-web-browser';
import * as Google from 'expo-auth-session/providers/google';
import { publicEnv } from '../env/publicEnv';
import { buildGoogleExpoGoAuthConfig } from '../auth/googleExpoGoAuthConfig';
import type { GoogleSignInButtonProps } from './GoogleSignInButton.types';

WebBrowser.maybeCompleteAuthSession();

/**
 * Google OAuth via browser (expo-auth-session). Works in Expo Go; does not use RNGoogleSignin.
 * For production, prefer the native SDK (`GoogleSignInButtonImpl`) in a dev build.
 */
export default function GoogleSignInExpoGo({ colors, style, onIdToken, onError }: GoogleSignInButtonProps) {
  const [pending, setPending] = useState(false);
  const webClientId = publicEnv('EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID');
  const { config, redirectUri } = useMemo(() => buildGoogleExpoGoAuthConfig(), []);

  useEffect(() => {
    if (__DEV__) {
      console.warn('[Google OAuth] clientId:', config.clientId);
      console.warn('[Google OAuth] redirectUri:', redirectUri);
      if (redirectUri.startsWith('https://auth.expo.io')) {
        console.warn(
          '[Google OAuth] auth.expo.io is deprecated. Prefer EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID (iOS) or npx expo run:ios.'
        );
      }
    }
  }, [config.clientId, redirectUri]);

  const [request, response, promptAsync] = Google.useIdTokenAuthRequest(config);

  useEffect(() => {
    if (response?.type === 'error') {
      setPending(false);
      onError(response.error?.message ?? 'Google sign-in failed');
      return;
    }
    if (response?.type === 'dismiss' || response?.type === 'cancel') {
      setPending(false);
      return;
    }
    if (response?.type === 'success') {
      const idToken =
        (typeof response.params?.id_token === 'string' && response.params.id_token) ||
        response.authentication?.idToken;
      setPending(false);
      if (idToken) onIdToken(idToken);
      else
        onError(
          'Google did not return an ID token. On iOS set EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID, or add the redirect URI from Metro to your Web OAuth client.'
        );
    }
  }, [response, onError, onIdToken]);

  const waitingForRequest = !request;

  return (
    <TouchableOpacity
      style={[
        {
          flex: 1,
          flexDirection: 'row',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 8,
          paddingVertical: 14,
          borderRadius: 12,
          borderWidth: 1,
          backgroundColor: colors.bg.default,
          borderColor: colors.border,
          opacity: waitingForRequest ? 0.65 : 1,
        },
        style,
      ]}
      disabled={pending}
      onPress={() => {
        if (!webClientId) {
          onError('Missing EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID in .env');
          return;
        }
        if (!request) {
          onError('Google sign-in is still loading. Restart Expo with: npx expo start -c');
          return;
        }
        setPending(true);
        void promptAsync()
          .then((result) => {
            if (result.type === 'locked') {
              setPending(false);
              onError('Another sign-in is in progress. Close the browser window and try again.');
            }
          })
          .catch((e: unknown) => {
            setPending(false);
            onError(e instanceof Error ? e.message : 'Google sign-in failed');
          });
      }}
      activeOpacity={0.7}
    >
      {pending || waitingForRequest ? (
        <ActivityIndicator color={colors.text.primary} />
      ) : (
        <>
          <Text style={{ fontSize: 18, fontWeight: '700', color: colors.text.primary }}>G</Text>
          <Text style={{ fontSize: 15, fontWeight: '500', color: colors.text.primary }}>Google</Text>
        </>
      )}
    </TouchableOpacity>
  );
}
