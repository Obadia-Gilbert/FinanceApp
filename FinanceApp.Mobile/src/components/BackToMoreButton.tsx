import { TouchableOpacity, Text, StyleSheet, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useRouter } from 'expo-router';
import { useTheme } from '../context/ThemeContext';

/** Back button that navigates to the More tab (used for screens opened from More menu). */
export function BackToMoreButton() {
  const { colors } = useTheme();
  const router = useRouter();
  const tint = colors.text.primary;

  return (
    <TouchableOpacity
      onPress={() => router.replace('/(tabs)/more')}
      style={styles.wrap}
      activeOpacity={0.7}
      accessibilityLabel="Back to More"
      accessibilityRole="button"
      hitSlop={{ top: 12, bottom: 12, left: 12, right: 12 }}
    >
      <View style={styles.row}>
        <Ionicons name="chevron-back" size={26} color={tint} style={styles.chevron} />
        <Text style={[styles.label, { color: tint }]}>Back</Text>
      </View>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  wrap: {
    marginLeft: 4,
    paddingVertical: 6,
    paddingRight: 8,
    minWidth: 44,
    minHeight: 44,
    justifyContent: 'center',
  },
  row: { flexDirection: 'row', alignItems: 'center' },
  chevron: { marginLeft: -6, marginRight: -2 },
  label: { fontSize: 17, fontWeight: '600' },
});
