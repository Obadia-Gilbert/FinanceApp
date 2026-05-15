import type { StyleProp, ViewStyle } from 'react-native';
import type { ThemeColors } from '../theme/colors';

export type GoogleSignInButtonProps = {
  colors: ThemeColors;
  style?: StyleProp<ViewStyle>;
  onIdToken: (idToken: string) => void;
  onError: (message: string) => void;
};
