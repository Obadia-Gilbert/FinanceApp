import { View, Text, StyleSheet } from 'react-native';
import { useTheme } from '../context/ThemeContext';

interface HeaderScreenIconProps {
  char: string;
}

/** Renders a screen icon on the left side of the header (e.g. ¢ for Expenses, ◉ for Budget). */
export function HeaderScreenIcon({ char }: HeaderScreenIconProps) {
  const { colors } = useTheme();
  return (
    <View style={styles.wrap}>
      <Text style={[styles.icon, { color: colors.text.primary }]}>{char}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    marginLeft: 8,
    padding: 8,
    justifyContent: 'center',
    alignItems: 'center',
  },
  icon: { fontSize: 22 },
});
