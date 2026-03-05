/**
 * Design tokens — clean modern UI (light blue-grey bg, blue accents)
 * Aligned with reference screens: Login welcome card, Dashboard, Expenses list.
 */
export const light = {
  brand: '#2563EB',
  brandHover: '#1D4ED8',
  brandLight: '#EFF6FF',
  text: {
    primary: '#111827',
    body: '#374151',
    muted: '#6B7280',
    subtle: '#9CA3AF',
  },
  bg: {
    default: '#ffffff',
    alt: '#F3F4F6',
    hover: '#E5E7EB',
    card: '#ffffff',
  },
  border: '#E5E7EB',
  borderFocus: '#93C5FD',
  success: '#10B981',
  danger: '#EF4444',
  warning: '#F59E0B',
  info: '#3B82F6',
  radius: { sm: 6, md: 8, lg: 12, xl: 16 },
  /** Welcome card gradient (login) */
  welcomeGradient: ['#EFF6FF', '#DBEAFE'] as const,
} as const;

export const dark = {
  brand: '#3B82F6',
  brandHover: '#2563EB',
  brandLight: '#1E3A5F',
  text: {
    primary: '#F9FAFB',
    body: '#D1D5DB',
    muted: '#9CA3AF',
    subtle: '#6B7280',
  },
  bg: {
    default: '#1a1a1a',
    alt: '#111111',
    hover: '#2a2a2a',
    card: '#1a1a1a',
  },
  border: '#2E2E2E',
  borderFocus: '#60A5FA',
  success: '#10B981',
  danger: '#EF4444',
  warning: '#F59E0B',
  info: '#3B82F6',
  radius: { sm: 6, md: 8, lg: 12, xl: 16 },
  welcomeGradient: ['#1E3A5F', '#0D1B2A'] as const,
} as const;

export type ThemeColors = typeof light;
