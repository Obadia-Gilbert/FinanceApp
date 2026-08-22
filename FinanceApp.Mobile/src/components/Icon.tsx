import { Ionicons } from '@expo/vector-icons';
import type { StyleProp, TextStyle } from 'react-native';

/**
 * The app's single icon vocabulary.
 *
 * Screens previously used emoji (💸 💰 📊 🎯 …) and stray typographic glyphs
 * (▣ ¢ ◉ ☰) as icons. Those render differently on every OS version, can't take
 * a colour, ignore the type scale, and read to a screen reader as their literal
 * character name. One outlined set fixes all four.
 *
 * Screens refer to icons by what they *mean* ("expense", "budget"), not by the
 * glyph name, so the underlying set can change in one place.
 */
export const iconNames = {
  // Navigation
  dashboard: 'grid-outline',
  expense: 'arrow-up-circle-outline',
  income: 'arrow-down-circle-outline',
  transactions: 'swap-horizontal-outline',
  recurring: 'repeat-outline',
  budget: 'pie-chart-outline',
  report: 'document-text-outline',
  categories: 'pricetags-outline',
  accounts: 'business-outline',
  accountSavings: 'save-outline',
  accountCreditCard: 'card-outline',
  accountCash: 'cash-outline',
  accountInvestment: 'trending-up-outline',
  more: 'ellipsis-horizontal',
  profile: 'person-outline',
  subscription: 'star-outline',
  notifications: 'notifications-outline',
  feedback: 'chatbubble-ellipses-outline',
  help: 'help-circle-outline',
  idea: 'bulb-outline',
  privacy: 'lock-closed-outline',
  language: 'globe-outline',
  signOut: 'log-out-outline',

  // Actions
  add: 'add',
  edit: 'pencil',
  delete: 'trash-outline',
  close: 'close',
  back: 'chevron-back',
  forward: 'chevron-forward',
  down: 'chevron-down',
  search: 'search-outline',
  filter: 'funnel-outline',
  camera: 'camera-outline',
  attachment: 'attach-outline',
  refresh: 'refresh',

  // States & meaning
  warning: 'warning-outline',
  info: 'information-circle-outline',
  success: 'checkmark-circle-outline',
  check: 'checkmark',
  error: 'alert-circle-outline',
  empty: 'file-tray-outline',
  wallet: 'wallet-outline',
  trendUp: 'trending-up',
  trendDown: 'trending-down',
  calendar: 'calendar-outline',
  security: 'shield-checkmark-outline',
  settings: 'settings-outline',
  clock: 'time-outline',
  cookie: 'nutrition-outline',
  external: 'open-outline',
  mail: 'mail-outline',
  lock: 'lock-closed-outline',
  key: 'key-outline',
} as const;

export type IconName = keyof typeof iconNames;

type IconProps = {
  name: IconName;
  size?: number;
  color?: string;
  style?: StyleProp<TextStyle>;
};

export function Icon({ name, size = 20, color, style }: IconProps) {
  return (
    <Ionicons
      name={iconNames[name] as React.ComponentProps<typeof Ionicons>['name']}
      size={size}
      color={color}
      style={style}
      // Icons here are always paired with a visible label or an accessible
      // label on the pressable that wraps them, so they are decorative.
      accessibilityElementsHidden
      importantForAccessibility="no"
    />
  );
}
