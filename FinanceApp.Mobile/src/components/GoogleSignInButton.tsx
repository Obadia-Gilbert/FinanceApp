import { lazy, Suspense } from 'react';
import {
  Platform,
  Text,
  TouchableOpacity,
  ActivityIndicator,
  Alert,
  type StyleProp,
  type ViewStyle,
} from 'react-native';
import Constants, { ExecutionEnvironment } from 'expo-constants';
import type { GoogleSignInButtonProps } from './GoogleSignInButton.types';

/** Browser OAuth — safe in Expo Go (no RNGoogleSignin). */
const GoogleSignInExpoGo = lazy(() => import('./GoogleSignInExpoGo'));
/** Native Google Sign-In — dev / release builds only. */
const GoogleSignInButtonNative = lazy(() => import('./GoogleSignInButtonImpl'));

function GoogleSignInUnavailable({
  colors,
  style,
  reason,
}: GoogleSignInButtonProps & { reason: string }) {
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
      onPress={() => Alert.alert('Google Sign-In', reason)}
      activeOpacity={0.7}
    >
      <Text style={{ fontSize: 18, fontWeight: '700', color: colors.text.primary }}>G</Text>
      <Text style={{ fontSize: 15, fontWeight: '500', color: colors.text.primary }}>Google</Text>
    </TouchableOpacity>
  );
}

function SignInFallback({ colors, style }: Pick<GoogleSignInButtonProps, 'colors' | 'style'>) {
  return (
    <TouchableOpacity
      style={[
        {
          flex: 1,
          flexDirection: 'row',
          alignItems: 'center',
          justifyContent: 'center',
          paddingVertical: 14,
          borderRadius: 12,
          borderWidth: 1,
          backgroundColor: colors.bg.default,
          borderColor: colors.border,
        },
        style as StyleProp<ViewStyle>,
      ]}
      disabled
    >
      <ActivityIndicator color={colors.text.primary} />
    </TouchableOpacity>
  );
}

/**
 * Expo Go: lazy-loads expo-auth-session (browser OAuth). Dev / release native: lazy-loads RNGoogleSignin.
 */
export function GoogleSignInButton(props: GoogleSignInButtonProps) {
  const expoGo = Constants.executionEnvironment === ExecutionEnvironment.StoreClient;
  const isWeb = Platform.OS === 'web';

  if (isWeb) {
    return (
      <GoogleSignInUnavailable
        {...props}
        reason="Google Sign-In is only supported in the iOS and Android apps, not in the browser preview."
      />
    );
  }

  if (expoGo) {
    return (
      <Suspense fallback={<SignInFallback colors={props.colors} style={props.style} />}>
        <GoogleSignInExpoGo {...props} />
      </Suspense>
    );
  }

  return (
    <Suspense fallback={<SignInFallback colors={props.colors} style={props.style} />}>
      <GoogleSignInButtonNative {...props} />
    </Suspense>
  );
}
