import React from 'react';
import {
  View,
  TextInput,
  Text,
  StyleSheet,
  TextInputProps,
  ViewStyle,
} from 'react-native';
import { useTheme } from '../context/ThemeContext';

interface InputProps extends TextInputProps {
  label?: string;
  error?: string;
  containerStyle?: ViewStyle;
}

export function Input({
  label,
  error,
  containerStyle,
  style,
  ...rest
}: InputProps) {
  const { colors } = useTheme();
  return (
    <View style={[styles.wrap, containerStyle]}>
      {label ? (
        <Text style={[styles.label, { color: colors.text.body }]}>{label}</Text>
      ) : null}
      <TextInput
        placeholderTextColor={colors.text.subtle}
        style={[
          styles.input,
          {
            backgroundColor: colors.bg.hover,
            borderColor: error ? colors.danger : colors.border,
            color: colors.text.primary,
          },
          style,
        ]}
        {...rest}
      />
      {error ? (
        <Text style={[styles.error, { color: colors.danger }]}>{error}</Text>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { marginBottom: 16 },
  label: { fontSize: 14, fontWeight: '500', marginBottom: 6 },
  input: {
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: 14,
    paddingVertical: 12,
    fontSize: 16,
  },
  error: { fontSize: 12, marginTop: 4 },
});
