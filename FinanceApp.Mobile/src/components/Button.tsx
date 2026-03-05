import React from 'react';
import {
  TouchableOpacity,
  Text,
  StyleSheet,
  ActivityIndicator,
  ViewStyle,
  TextStyle,
} from 'react-native';
import { useTheme } from '../context/ThemeContext';

type Variant = 'primary' | 'secondary' | 'danger' | 'ghost';

interface ButtonProps {
  title: string;
  onPress: () => void;
  variant?: Variant;
  loading?: boolean;
  disabled?: boolean;
  style?: ViewStyle;
  textStyle?: TextStyle;
}

export function Button({
  title,
  onPress,
  variant = 'primary',
  loading,
  disabled,
  style,
  textStyle,
}: ButtonProps) {
  const { colors, isDark } = useTheme();

  const bg =
    variant === 'primary'
      ? colors.brand
      : variant === 'danger'
        ? colors.danger
        : variant === 'secondary'
          ? 'transparent'
          : 'transparent';
  const border = variant === 'secondary' || variant === 'ghost' ? colors.border : undefined;
  const textColor =
    variant === 'primary' || variant === 'danger'
      ? '#fff'
      : variant === 'secondary' || variant === 'ghost'
        ? colors.text.body
        : colors.text.body;

  return (
    <TouchableOpacity
      onPress={onPress}
      disabled={disabled || loading}
      activeOpacity={0.8}
      style={[
        styles.btn,
        { backgroundColor: bg, borderWidth: border ? 1 : 0, borderColor: border },
        (disabled || loading) && styles.disabled,
        style,
      ]}
    >
      {loading ? (
        <ActivityIndicator color={textColor} size="small" />
      ) : (
        <Text style={[styles.text, { color: textColor }, textStyle]}>{title}</Text>
      )}
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  btn: {
    minHeight: 44,
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 16,
  },
  disabled: { opacity: 0.6 },
  text: { fontSize: 16, fontWeight: '600' },
});
