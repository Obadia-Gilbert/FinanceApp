import { View, TouchableOpacity, Text, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { getUnreadCount } from '../api/notifications';
import { useTheme } from '../context/ThemeContext';
import { Icon } from './Icon';

interface HeaderNotificationIconProps {
  /** When 'left', use marginLeft (e.g. in headerLeft); when 'right', use marginRight (default). */
  align?: 'left' | 'right';
}

export function HeaderNotificationIcon({ align = 'right' }: HeaderNotificationIconProps) {
  const router = useRouter();
  const { colors } = useTheme();
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
      <Icon name="notifications" size={22} color={colors.text.body} />
      {showBadge && (
        <View style={styles.badge}>
          {/* No numberOfLines/ellipsis: inside the navigation header the badge is
              width-constrained, so a two- or three-digit count was rendering as
              "1..". The text must be allowed to size the pill instead. */}
          <Text style={styles.badgeText}>{badgeLabel}</Text>
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
  badge: {
    position: 'absolute',
    top: 0,
    // Negative inset lets the pill grow past the icon's box for 2–3 digit
    // counts; anchored at right: 2 it was clamped to the icon's own width.
    right: -2,
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
    // Keeps the digits from being compressed by the parent's cross-axis sizing.
    flexShrink: 0,
    textAlign: 'center',
  },
});
