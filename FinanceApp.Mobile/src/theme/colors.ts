/**
 * Design tokens — kept deliberately in step with the web app's
 * FinanceApp.Web/wwwroot/css/site.css so both surfaces read as one product.
 *
 * Palette comes from the product mark: a teal/emerald arc on a dark navy
 * squircle. Teal is the interactive accent; the dark theme's ground is the
 * mark's navy. Semantic colours (positive / negative / warning) are kept
 * separate from the brand accent so "this is tappable" never reads as
 * "this is income".
 *
 * If you change a value here, change its counterpart in site.css.
 */

/** Spacing scale, 4px base — mirrors --space-1..7 on web. */
export const spacing = {
  xs: 4,
  sm: 8,
  md: 12,
  base: 16,
  lg: 24,
  xl: 32,
  xxl: 48,
} as const;

/** Type scale — mirrors --text-xs..2xl on web. */
export const typeScale = {
  xs: 12,
  sm: 13,
  base: 15,
  md: 17,
  lg: 22,
  xl: 28,
  xxl: 36,
} as const;

const radiusScale = { sm: 6, md: 8, lg: 12, xl: 16, pill: 999 } as const;

/**
 * Explicit theme contract. Both palettes are annotated with it rather than
 * relying on `typeof light` from an `as const` object — that inferred *literal*
 * types ("#0D9488" rather than string), so the dark palette was not assignable
 * to the light one and any component holding a themed colour tripped over it.
 */
export type ThemeColors = {
  brand: string;
  brandHover: string;
  brandLight: string;
  brandContrast: string;
  text: { primary: string; body: string; muted: string; subtle: string };
  bg: { default: string; alt: string; hover: string; card: string; inset: string };
  border: string;
  borderStrong: string;
  borderFocus: string;
  success: string;
  successSoft: string;
  danger: string;
  dangerSoft: string;
  warning: string;
  warningSoft: string;
  info: string;
  infoSoft: string;
  radius: typeof radiusScale;
  spacing: typeof spacing;
  typeScale: typeof typeScale;
  welcomeGradient: readonly [string, string];
};

export const light: ThemeColors = {
  brand: '#0D9488',
  brandHover: '#0F766E',
  brandLight: '#E6F5F3',
  /** Text/icon colour that sits on top of a filled brand surface. */
  brandContrast: '#FFFFFF',
  text: {
    primary: '#0E1A1E',
    body: '#33454C',
    muted: '#5F757E',
    subtle: '#8CA0A8',
  },
  bg: {
    default: '#FFFFFF',
    alt: '#F4F7F7',
    hover: '#EDF2F2',
    card: '#FFFFFF',
    inset: '#F8FAFA',
  },
  border: '#DEE7E7',
  borderStrong: '#C6D3D3',
  borderFocus: '#0D9488',
  success: '#15803D',
  successSoft: '#E7F5EC',
  danger: '#C81E1E',
  dangerSoft: '#FCEBEB',
  warning: '#B45309',
  warningSoft: '#FBF0E2',
  info: '#1D4ED8',
  infoSoft: '#EBF1FE',
  radius: radiusScale,
  spacing,
  typeScale,
  /** Welcome card gradient (login) */
  welcomeGradient: ['#E6F5F3', '#CDEBE7'] as const,
};

export const dark: ThemeColors = {
  brand: '#2DD4BF',
  brandHover: '#5EEAD4',
  brandLight: '#12312F',
  brandContrast: '#04211D',
  text: {
    primary: '#F1F6F7',
    body: '#C2D0D6',
    muted: '#8CA0AA',
    subtle: '#6A7C86',
  },
  bg: {
    default: '#171F27',
    alt: '#0F151B',
    hover: '#1F2A33',
    card: '#171F27',
    inset: '#131B22',
  },
  border: '#29363F',
  borderStrong: '#3A4A55',
  borderFocus: '#2DD4BF',
  success: '#4ADE80',
  successSoft: '#10291A',
  danger: '#F87171',
  dangerSoft: '#2C1516',
  warning: '#FBBF24',
  warningSoft: '#2C2110',
  info: '#60A5FA',
  infoSoft: '#12203A',
  radius: radiusScale,
  spacing,
  typeScale,
  welcomeGradient: ['#12312F', '#0B1A19'],
};
