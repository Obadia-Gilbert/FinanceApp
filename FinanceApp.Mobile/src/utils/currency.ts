/** All supported currencies (order matches API Currency enum: USD=0, EUR=1, TZS=2, …) */
export const CURRENCY_LIST = ['USD', 'EUR', 'TZS', 'GBP', 'JPY', 'AUD', 'CAD', 'CHF', 'UGX', 'KES', 'RWF', 'ZAR', 'CNY', 'INR', 'BRL', 'MXN'];

/** Index of currency code in CURRENCY_LIST (for API enum). */
export function getCurrencyIndex(currency: string | number | undefined): number {
  if (currency == null) return 0;
  if (typeof currency === 'number') return Math.max(0, Math.min(currency, CURRENCY_LIST.length - 1));
  const i = CURRENCY_LIST.indexOf(currency);
  return i >= 0 ? i : 0;
}

export function formatCurrencyCode(currency: string | number | undefined): string {
  if (currency == null) return '';
  if (typeof currency === 'string') return currency;
  return CURRENCY_LIST[currency] ?? String(currency);
}
