import { View, TouchableOpacity, Text, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { getUnreadCount } from '../api/notifications';

interface HeaderNotificationIconProps {
  /** When 'left', use marginLeft (e.g. in headerLeft); when 'right', use marginRight (default). */
  align?: 'left' | 'right';
}

export function HeaderNotificationIcon({ align = 'right' }: HeaderNotificationIconProps) {
  const router = useRouter();
  const { data: unreadCount = 0 } = useQuery({
    queryKey: ['notificationsUnreadCount'],
    queryFn: getUnreadCount,
    staleTime: 60 * 1000,
  });

  const showBadge = unreadCount > 0;
  const badgeLabel = unreadCount > 99 ? '99+' : String(unreadCount);

  return (
    <TouchableOpacity
      onPress={() => router.push('/(tabs)/notifications')}
      style={[styles.wrap, align === 'left' ? styles.wrapLeft : styles.wrapRight]}
      activeOpacity={0.7}
      accessibilityLabel={showBadge ? `Notifications (${unreadCount} unread)` : 'Notifications'}
      hitSlop={{ top: 12, bottom: 12, left: 12, right: 12 }}
    >
      <Text style={styles.icon}>🔔</Text>
      {showBadge && (
        <View style={styles.badge}>
          <Text style={styles.badgeText} numberOfLines={1}>
            {badgeLabel}
          </Text>
        </View>
      )}
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  wrap: {
    padding: 8,
    justifyContent: 'center',
    alignItems: 'center',
    position: 'relative',
  },
  wrapLeft: { marginLeft: 8 },
  wrapRight: { marginRight: 16 },
  icon: { fontSize: 22 },
  badge: {
    position: 'absolute',
    top: 2,
    right: 2,
    minWidth: 18,
    height: 18,
    borderRadius: 9,
    backgroundColor: '#DC2626',
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 5,
  },
  badgeText: {
    color: '#fff',
    fontSize: 11,
    fontWeight: '700',
  },
});
