/** All supported currencies. Display order for picker UI only — not an API contract. */
export const CURRENCY_LIST = ['USD', 'EUR', 'TZS', 'GBP', 'JPY', 'AUD', 'CAD', 'CHF', 'UGX', 'KES', 'RWF', 'ZAR', 'CNY', 'INR', 'BRL', 'MXN'];

export function formatCurrencyCode(currency: string | undefined): string {
  return currency ?? '';
}

/** ISO 4217 currencies with zero minor units (no decimal places). */
const ZERO_DECIMAL_CURRENCIES = new Set(['JPY']);

export function getCurrencyDecimals(currencyCode: string | undefined): number {
  if (!currencyCode) return 2;
  return ZERO_DECIMAL_CURRENCIES.has(currencyCode) ? 0 : 2;
}

/** Formats a monetary amount with the correct number of decimal places for its currency. */
export function formatAmount(amount: number, currencyCode: string | undefined): string {
  const decimals = getCurrencyDecimals(currencyCode);
  return amount.toLocaleString(undefined, { minimumFractionDigits: decimals, maximumFractionDigits: decimals });
}
