import { Alert, Text, TouchableOpacity } from 'react-native';
import type { ThemeColors } from '../theme/colors';

const GOOGLE_SETUP =
  'Add EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID to .env (Web application OAuth client from Google Cloud), then restart Expo. For iOS development builds with the native Google SDK, also set EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID (bundle com.financeapp.mobile). Add test users on the OAuth consent screen if the app is in Testing.';

const FACEBOOK_SETUP =
  'Add EXPO_PUBLIC_FACEBOOK_APP_ID to .env (same App ID as FinanceApp.Web Authentication:Facebook:AppId), then restart Expo. See env.example.';

type Props = {
  provider: 'google' | 'facebook';
  colors: ThemeColors;
};

export function SocialLoginStub({ provider, colors }: Props) {
  const isGoogle = provider === 'google';
  return (
    <TouchableOpacity
      style={{
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
      }}
      onPress={() =>
        Alert.alert(
          isGoogle ? 'Google sign-in' : 'Facebook sign-in',
          isGoogle ? GOOGLE_SETUP : FACEBOOK_SETUP
        )
      }
      activeOpacity={0.7}
    >
      <Text style={{ fontSize: 18, fontWeight: '700', color: colors.text.primary }}>
        {isGoogle ? 'G' : 'f'}
      </Text>
      <Text style={{ fontSize: 15, fontWeight: '500', color: colors.text.primary }}>
        {isGoogle ? 'Google' : 'Facebook'}
      </Text>
    </TouchableOpacity>
  );
}
