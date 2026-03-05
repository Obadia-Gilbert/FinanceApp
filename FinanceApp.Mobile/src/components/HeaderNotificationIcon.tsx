import { TouchableOpacity, Text, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';

interface HeaderNotificationIconProps {
  /** When 'left', use marginLeft (e.g. in headerLeft); when 'right', use marginRight (default). */
  align?: 'left' | 'right';
}

export function HeaderNotificationIcon({ align = 'right' }: HeaderNotificationIconProps) {
  const router = useRouter();

  return (
    <TouchableOpacity
      onPress={() => router.push('/(tabs)/notifications')}
      style={[styles.wrap, align === 'left' ? styles.wrapLeft : styles.wrapRight]}
      activeOpacity={0.7}
      accessibilityLabel="Notifications"
      hitSlop={{ top: 12, bottom: 12, left: 12, right: 12 }}
    >
      <Text style={styles.icon}>🔔</Text>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  wrap: {
    padding: 8,
    justifyContent: 'center',
    alignItems: 'center',
  },
  wrapLeft: { marginLeft: 8 },
  wrapRight: { marginRight: 16 },
  icon: { fontSize: 22 },
});
