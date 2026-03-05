import { TouchableOpacity, Text, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';
import { useTheme } from '../context/ThemeContext';

export function HeaderBackButton() {
  const { colors } = useTheme();
  const router = useRouter();

  return (
    <TouchableOpacity
      onPress={() => router.back()}
      style={styles.wrap}
      activeOpacity={0.7}
      accessibilityLabel="Go back"
      hitSlop={{ top: 12, bottom: 12, left: 12, right: 12 }}
    >
      <Text style={[styles.arrow, { color: colors.brand }]}>←</Text>
      <Text style={[styles.label, { color: colors.brand }]}>Back</Text>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  wrap: {
    flexDirection: 'row',
    alignItems: 'center',
    marginLeft: 8,
    paddingVertical: 8,
    paddingRight: 12,
  },
  arrow: { fontSize: 22, fontWeight: '600', marginRight: 4 },
  label: { fontSize: 16, fontWeight: '600' },
});
