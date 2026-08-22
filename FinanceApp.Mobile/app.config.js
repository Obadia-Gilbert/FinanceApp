// Dynamic Expo config: Facebook `fb{AppId}` scheme + Google Sign-In config plugin (native SDK).
const base = require('./app.json');

function googleReversedUrlScheme(clientId) {
  const id = clientId?.trim();
  if (!id || !id.endsWith('.apps.googleusercontent.com')) return null;
  const prefix = id.slice(0, -'.apps.googleusercontent.com'.length);
  return `com.googleusercontent.apps.${prefix}`;
}

module.exports = () => {
  const fbId = process.env.EXPO_PUBLIC_FACEBOOK_APP_ID?.trim();
  const iosClientId = process.env.EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID?.trim();
  const webClientId = process.env.EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID?.trim();
  const androidClientId = process.env.EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID?.trim();

  const schemes = ['financeapp'];
  if (fbId) schemes.push(`fb${fbId}`);

  // Native iOS Google Sign-In requires the reversed iOS client ID (not the Web client).
  // Without EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID the native plugin is omitted; Expo Go (browser flow) still works.
  const googleIosUrlScheme = googleReversedUrlScheme(iosClientId);

  const plugins = [...(base.expo.plugins || [])];
  if (googleIosUrlScheme) {
    plugins.push([
      '@react-native-google-signin/google-signin',
      { iosUrlScheme: googleIosUrlScheme },
    ]);
  }

  const expoOwner = process.env.EXPO_PUBLIC_EXPO_OWNER?.trim();

  return {
    expo: {
      ...base.expo,
      ...(expoOwner ? { owner: expoOwner } : {}),
      scheme: schemes.length === 1 ? schemes[0] : schemes,
      plugins,
      extra: {
        ...(base.expo.extra ?? {}),
        EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID: webClientId ?? '',
        EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID: iosClientId ?? '',
        EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID: androidClientId ?? '',
        EXPO_PUBLIC_FACEBOOK_APP_ID: fbId ?? '',
      },
    },
  };
};
