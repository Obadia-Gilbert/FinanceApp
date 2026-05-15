import { useEffect, useState } from 'react';
import { Text, TouchableOpacity, ActivityIndicator } from 'react-native';
import * as WebBrowser from 'expo-web-browser';
import * as Google from 'expo-auth-session/providers/google';
import type { GoogleSignInButtonProps } from './GoogleSignInButton.types';

WebBrowser.maybeCompleteAuthSession();

/**
 * Google OAuth via browser (expo-auth-session). Works in Expo Go; does not use RNGoogleSignin.
 * For production, prefer the native SDK (`GoogleSignInButtonImpl`) in a dev build.
 */
export default function GoogleSignInExpoGo({ colors, style, onIdToken, onError }: GoogleSignInButtonProps) {
  const [pending, setPending] = useState(false);
  const webClientId = process.env.EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID;
  const [request, response, promptAsync] = Google.useIdTokenAuthRequest({
    iosClientId: process.env.EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID,
    androidClientId: process.env.EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID,
    webClientId,
    /** Fallback when platform-specific OAuth client ID is not set (e.g. Android + web-only env). */
    clientId: webClientId,
  });

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
          'Google did not return an ID token. Add authorized redirect URIs in Google Cloud (including Expo proxy if shown in the browser error).'
        );
    }
  }, [response, onError, onIdToken]);

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
        },
        style,
      ]}
      disabled={!request || pending}
      onPress={() => {
        setPending(true);
        void promptAsync().catch(() => setPending(false));
      }}
      activeOpacity={0.7}
    >
      {pending ? (
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
