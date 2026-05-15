import { useEffect, useState } from 'react';
import { Text, TouchableOpacity, ActivityIndicator, Platform, type StyleProp, type ViewStyle } from 'react-native';
import { useAuthRequest, ResponseType, makeRedirectUri } from 'expo-auth-session';
import type { ThemeColors } from '../theme/colors';

// expo-auth-session/providers/facebook hardcodes Graph API v6.0 (retired by Meta), which makes the dialog show
// "The link you followed may be broken". Drive the request ourselves against a supported version.
const FACEBOOK_API_VERSION = 'v21.0';
const FACEBOOK_DISCOVERY = {
  authorizationEndpoint: `https://www.facebook.com/${FACEBOOK_API_VERSION}/dialog/oauth`,
  tokenEndpoint: `https://graph.facebook.com/${FACEBOOK_API_VERSION}/oauth/access_token`,
};

type Props = {
  clientId: string;
  colors: ThemeColors;
  style?: StyleProp<ViewStyle>;
  onAccessToken: (accessToken: string) => void;
  onError: (message: string) => void;
};

export function FacebookSignInButton({ clientId, colors, style, onAccessToken, onError }: Props) {
  const [pending, setPending] = useState(false);
  // Native: Meta's expected app redirect (iOS/Android URL scheme registered by app.config.js).
  // Web: standard browser redirect via makeRedirectUri.
  const redirectUri =
    Platform.OS === 'web' ? makeRedirectUri() : `fb${clientId}://authorize`;
  const [request, response, promptAsync] = useAuthRequest(
    {
      clientId,
      responseType: ResponseType.Token,
      usePKCE: false,
      scopes: ['public_profile', 'email'],
      redirectUri,
      extraParams: {
        display: Platform.OS === 'web' ? 'popup' : 'touch',
      },
    },
    FACEBOOK_DISCOVERY,
  );

  useEffect(() => {
    if (response?.type === 'error') {
      setPending(false);
      const fromProvider =
        typeof response.params?.error_description === 'string'
          ? response.params.error_description
          : undefined;
      onError(
        (fromProvider && fromProvider.trim()) ||
          response.error?.message ||
          'Facebook sign-in failed',
      );
      return;
    }
    if (response?.type === 'dismiss' || response?.type === 'cancel') {
      setPending(false);
      return;
    }
    if (response?.type === 'success') {
      const token =
        response.authentication?.accessToken ||
        (typeof response.params?.access_token === 'string' ? response.params.access_token : undefined);
      setPending(false);
      if (token) onAccessToken(token);
      else onError('Facebook did not return an access token.');
    }
  }, [response, onError, onAccessToken]);

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
        void promptAsync({
          showInRecents: Platform.OS === 'android',
        }).finally(() => {});
      }}
      activeOpacity={0.7}
    >
      {pending ? (
        <ActivityIndicator color={colors.text.primary} />
      ) : (
        <>
          <Text style={{ fontSize: 18, fontWeight: '700', color: colors.text.primary }}>f</Text>
          <Text style={{ fontSize: 15, fontWeight: '500', color: colors.text.primary }}>Facebook</Text>
        </>
      )}
    </TouchableOpacity>
  );
}
