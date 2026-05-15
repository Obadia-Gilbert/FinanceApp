import { useEffect, useState } from 'react';
import {
  Platform,
  Text,
  TouchableOpacity,
  ActivityIndicator,
  type StyleProp,
  type ViewStyle,
} from 'react-native';
import {
  GoogleSignin,
  statusCodes,
  isErrorWithCode,
} from '@react-native-google-signin/google-signin';
import type { GoogleSignInButtonProps } from './GoogleSignInButton.types';

export default function GoogleSignInButtonImpl({
  colors,
  style,
  onIdToken,
  onError,
}: GoogleSignInButtonProps) {
  const [pending, setPending] = useState(false);

  useEffect(() => {
    const web = process.env.EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID?.trim();
    const ios = process.env.EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID?.trim();
    if (!web) return;

    GoogleSignin.configure({
      webClientId: web,
      ...(Platform.OS === 'ios' && ios ? { iosClientId: ios } : {}),
      offlineAccess: false,
    });
  }, []);

  const handlePress = async () => {
    const web = process.env.EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID?.trim();
    if (!web) {
      onError('Missing EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID in .env');
      return;
    }
    if (Platform.OS === 'ios' && !process.env.EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID?.trim()) {
      onError(
        'Missing EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID — add an iOS OAuth client from Google Cloud (bundle com.financeapp.mobile).'
      );
      return;
    }

    setPending(true);
    try {
      if (Platform.OS === 'android') {
        await GoogleSignin.hasPlayServices({ showPlayServicesUpdateDialog: true });
      }

      const response = await GoogleSignin.signIn();
      if (response.type !== 'success') {
        return;
      }

      const tokens = await GoogleSignin.getTokens();
      if (tokens.idToken) {
        onIdToken(tokens.idToken);
      } else {
        onError(
          'Google did not return an ID token. Check Web client ID in Google Cloud and OAuth consent screen (add test users if app is in Testing).'
        );
      }
    } catch (e: unknown) {
      if (isErrorWithCode(e) && e.code === statusCodes.SIGN_IN_CANCELLED) {
        return;
      }
      const msg = e instanceof Error ? e.message : 'Google sign-in failed';
      onError(msg);
    } finally {
      setPending(false);
    }
  };

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
      disabled={pending}
      onPress={() => void handlePress()}
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
