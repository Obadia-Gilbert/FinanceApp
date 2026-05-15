import { Platform } from 'react-native';

/** Store product IDs — override via EXPO_PUBLIC_IAP_* in .env (must match App Store Connect / Play Console). */
export const IAP_PRODUCT_IDS = {
  ios: {
    pro: process.env.EXPO_PUBLIC_IAP_IOS_PRO ?? 'com.financeapp.mobile.pro.monthly',
    premium: process.env.EXPO_PUBLIC_IAP_IOS_PREMIUM ?? 'com.financeapp.mobile.premium.monthly',
  },
  android: {
    pro: process.env.EXPO_PUBLIC_IAP_ANDROID_PRO ?? 'pro_monthly',
    premium: process.env.EXPO_PUBLIC_IAP_ANDROID_PREMIUM ?? 'premium_monthly',
  },
} as const;

export type IapTier = 'pro' | 'premium';

export function getStoreProductId(tier: IapTier): string {
  if (Platform.OS === 'ios') {
    return tier === 'pro' ? IAP_PRODUCT_IDS.ios.pro : IAP_PRODUCT_IDS.ios.premium;
  }
  return tier === 'pro' ? IAP_PRODUCT_IDS.android.pro : IAP_PRODUCT_IDS.android.premium;
}

export function getAllSubscriptionSkus(): string[] {
  if (Platform.OS === 'ios') {
    return [IAP_PRODUCT_IDS.ios.pro, IAP_PRODUCT_IDS.ios.premium];
  }
  return [IAP_PRODUCT_IDS.android.pro, IAP_PRODUCT_IDS.android.premium];
}
