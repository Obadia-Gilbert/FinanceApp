import { apiFetch } from './client';
import type { SubscriptionDto } from '../types/api';

/** After StoreKit 2 purchase, send the signed transaction JWS to the API. */
export async function verifyApplePurchase(signedTransactionJws: string): Promise<SubscriptionDto> {
  return apiFetch<SubscriptionDto>('/api/subscription/verify/apple', {
    method: 'POST',
    body: JSON.stringify({ signedTransactionJws }),
  });
}

/** After Google Play purchase, send subscription id + purchase token. */
export async function verifyGooglePurchase(
  subscriptionId: string,
  purchaseToken: string
): Promise<SubscriptionDto> {
  return apiFetch<SubscriptionDto>('/api/subscription/verify/google', {
    method: 'POST',
    body: JSON.stringify({ subscriptionId, purchaseToken }),
  });
}
