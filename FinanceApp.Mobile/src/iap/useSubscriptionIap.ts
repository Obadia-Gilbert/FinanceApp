import { useCallback, useEffect, useRef, useState } from 'react';
import { Platform } from 'react-native';
import { useQueryClient } from '@tanstack/react-query';
import { verifyApplePurchase, verifyGooglePurchase } from '../api/subscriptionBilling';
import { getAllSubscriptionSkus, getStoreProductId, type IapTier } from './config';

type IapModule = typeof import('react-native-iap');

export type SubscriptionProduct = {
  productId: string;
  title: string;
  description: string;
  localizedPrice?: string;
};

export function useSubscriptionIap() {
  const queryClient = useQueryClient();
  const iapRef = useRef<IapModule | null>(null);
  const [available, setAvailable] = useState(false);
  const [loading, setLoading] = useState(false);
  const [products, setProducts] = useState<SubscriptionProduct[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let purchaseSub: { remove: () => void } | undefined;
    let errorSub: { remove: () => void } | undefined;
    let mounted = true;

    (async () => {
      try {
        const iap = await import('react-native-iap');
        iapRef.current = iap;

        await iap.initConnection();
        if (Platform.OS === 'android') {
          await iap.flushFailedPurchasesCachedAsPendingAndroid().catch(() => undefined);
        }

        const skus = getAllSubscriptionSkus();
        const subs = await iap.getSubscriptions({ skus });
        if (!mounted) return;

        setProducts(
          subs.map((s) => ({
            productId: s.productId,
            title: s.title ?? s.productId,
            description: s.description ?? '',
            localizedPrice:
              'localizedPrice' in s ? (s as { localizedPrice?: string }).localizedPrice : undefined,
          }))
        );
        setAvailable(true);

        purchaseSub = iap.purchaseUpdatedListener(async (purchase) => {
          try {
            await handlePurchase(iap, purchase);
            await queryClient.invalidateQueries({ queryKey: ['subscription'] });
          } catch (e) {
            setError(e instanceof Error ? e.message : 'Purchase verification failed');
          }
        });

        errorSub = iap.purchaseErrorListener((err) => {
          if (err.code !== 'E_USER_CANCELLED') {
            setError(err.message);
          }
        });
      } catch {
        if (mounted) {
          setAvailable(false);
          setError(
            'In-app purchases require a dev build (expo run:ios / run:android). Expo Go cannot load react-native-iap.'
          );
        }
      }
    })();

    return () => {
      mounted = false;
      purchaseSub?.remove();
      errorSub?.remove();
      iapRef.current?.endConnection().catch(() => undefined);
    };
  }, [queryClient]);

  const handlePurchase = async (
    iap: IapModule,
    purchase: import('react-native-iap').Purchase
  ) => {
    if (Platform.OS === 'ios') {
      const jws =
        (purchase as { purchaseToken?: string }).purchaseToken ??
        (purchase as { transactionReceipt?: string }).transactionReceipt;
      if (!jws) throw new Error('Missing Apple signed transaction from purchase.');
      await verifyApplePurchase(jws);
    } else {
      const token = purchase.purchaseToken;
      if (!token) throw new Error('Missing Google purchase token.');
      await verifyGooglePurchase(purchase.productId, token);
    }

    await iap.finishTransaction({ purchase, isConsumable: false });
  };

  const subscribe = useCallback(
    async (tier: IapTier) => {
      const iap = iapRef.current;
      if (!iap || !available) {
        throw new Error(error ?? 'Store not available');
      }

      setLoading(true);
      setError(null);
      try {
        const sku = getStoreProductId(tier);
        await iap.requestSubscription({ sku });
      } finally {
        setLoading(false);
      }
    },
    [available, error]
  );

  const restore = useCallback(async () => {
    const iap = iapRef.current;
    if (!iap || !available) {
      throw new Error(error ?? 'Store not available');
    }

    setLoading(true);
    setError(null);
    try {
      const purchases = await iap.getAvailablePurchases();
      if (purchases.length === 0) {
        throw new Error('No purchases to restore.');
      }

      for (const purchase of purchases) {
        await handlePurchase(iap, purchase);
      }

      await queryClient.invalidateQueries({ queryKey: ['subscription'] });
    } finally {
      setLoading(false);
    }
  }, [available, error, queryClient]);

  return {
    available,
    loading,
    products,
    error,
    subscribe,
    restore,
  };
}
