import { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  RefreshControl,
} from 'react-native';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTheme } from '../../src/context/ThemeContext';
import { Card } from '../../src/components/Card';
import {
  getNotifications,
  markNotificationRead,
  markAllNotificationsRead,
} from '../../src/api/notifications';
import type { NotificationItemDto } from '../../src/types/api';

function relativeTime(dateStr: string): string {
  const date = new Date(dateStr);
  const now = new Date();
  const sec = Math.floor((now.getTime() - date.getTime()) / 1000);
  if (sec < 60) return 'JUST NOW';
  if (sec < 3600) return `${Math.floor(sec / 60)}M AGO`;
  if (sec < 86400) return `${Math.floor(sec / 3600)}H AGO`;
  if (sec < 604800) return `${Math.floor(sec / 86400)}D AGO`;
  if (sec < 2592000) return `${Math.floor(sec / 604800)}W AGO`;
  return date.toLocaleDateString();
}

function iconForType(type: string): string {
  const t = (type || '').toLowerCase();
  if (t.includes('budget')) return '⚠️';
  if (t.includes('subscription') || t.includes('renew')) return 'ℹ️';
  if (t.includes('goal') || t.includes('savings')) return '✓';
  if (t.includes('balance') || t.includes('low')) return '👛';
  return '🔔';
}

function iconBgForType(type: string): string {
  const t = (type || '').toLowerCase();
  if (t.includes('budget')) return '#F59E0B';
  if (t.includes('subscription') || t.includes('renew')) return '#3B82F6';
  if (t.includes('goal') || t.includes('savings')) return '#10B981';
  if (t.includes('balance') || t.includes('low')) return '#EF4444';
  return '#6B7280';
}

export default function NotificationsScreen() {
  const { colors } = useTheme();
  const queryClient = useQueryClient();
  const [filter, setFilter] = useState<'all' | 'unread'>('all');

  const { data, refetch, isRefetching } = useQuery({
    queryKey: ['notifications'],
    queryFn: () => getNotifications(1, 50),
  });

  const list = data?.items ?? [];
  const unreadCount = list.filter((n) => !n.isRead).length;
  const filtered = filter === 'unread' ? list.filter((n) => !n.isRead) : list;

  const markReadMutation = useMutation({
    mutationFn: markNotificationRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['notifications'] }),
  });

  const markAllMutation = useMutation({
    mutationFn: markAllNotificationsRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['notifications'] }),
  });

  return (
    <View style={[styles.container, { backgroundColor: colors.bg.alt }]}>
      <View style={[styles.tabRow, { borderBottomColor: colors.border }]}>
        <View style={styles.tabs}>
          <TouchableOpacity style={[styles.tab, filter === 'all' && styles.tabActive]} onPress={() => setFilter('all')}>
            <Text style={[styles.tabText, { color: filter === 'all' ? colors.brand : colors.text.primary }]}>All</Text>
            {unreadCount > 0 && (
              <View style={[styles.badge, { backgroundColor: colors.brand }]}>
                <Text style={styles.badgeText}>{unreadCount}</Text>
              </View>
            )}
          </TouchableOpacity>
          <TouchableOpacity style={[styles.tab, filter === 'unread' && styles.tabActive]} onPress={() => setFilter('unread')}>
            <Text style={[styles.tabText, { color: filter === 'unread' ? colors.brand : colors.text.primary }]}>Unread</Text>
          </TouchableOpacity>
        </View>
        {unreadCount > 0 && (
          <TouchableOpacity onPress={() => markAllMutation.mutate()} disabled={markAllMutation.isPending}>
            <Text style={[styles.markAllText, { color: colors.brand }]}>Mark all as read</Text>
          </TouchableOpacity>
        )}
      </View>

      <FlatList
        data={filtered}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.listContent}
        refreshControl={
          <RefreshControl refreshing={isRefetching} onRefresh={() => refetch()} tintColor={colors.brand} />
        }
        ListEmptyComponent={
          <Card style={styles.empty}>
            <Text style={[styles.emptyTitle, { color: colors.text.primary }]}>No notifications</Text>
            <Text style={[styles.emptyBody, { color: colors.text.muted }]}>
              {filter === 'unread' ? "You're all caught up." : 'No notifications from the last 30 days.'}
            </Text>
          </Card>
        }
        renderItem={({ item }) => (
          <NotificationRow item={item} colors={colors} onMarkRead={() => markReadMutation.mutate(item.id)} />
        )}
      />

      {list.length > 0 && (
        <Text style={[styles.footerHint, { color: colors.text.muted }]}>
          No more notifications from the last 30 days.
        </Text>
      )}
    </View>
  );
}

function NotificationRow({
  item,
  colors,
  onMarkRead,
}: {
  item: NotificationItemDto;
  colors: ReturnType<typeof useTheme>['colors'];
  onMarkRead: () => void;
}) {
  const icon = iconForType(item.type);
  const iconBg = iconBgForType(item.type);
  const timeStr = relativeTime(item.createdAt);

  return (
    <Card style={[styles.row, !item.isRead && { backgroundColor: colors.bg.default }]}>
      <View style={[styles.iconWrap, { backgroundColor: iconBg }]}>
        <Text style={styles.iconText}>{icon}</Text>
      </View>
      <View style={styles.rowBody}>
        <Text style={[styles.rowTitle, { color: colors.text.primary }]}>{item.title}</Text>
        <Text style={[styles.rowMessage, { color: colors.text.body }]}>{item.message}</Text>
        {!item.isRead && (
          <TouchableOpacity onPress={onMarkRead} style={styles.readLink}>
            <Text style={[styles.readLinkText, { color: colors.brand }]}>Mark read</Text>
          </TouchableOpacity>
        )}
      </View>
      <Text style={[styles.rowTime, { color: colors.text.muted }]}>{timeStr}</Text>
    </Card>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  tabRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingTop: 8,
    paddingBottom: 12,
    borderBottomWidth: 1,
  },
  tabs: { flexDirection: 'row', alignItems: 'center' },
  tab: { flexDirection: 'row', alignItems: 'center', marginRight: 20, paddingVertical: 8 },
  tabActive: {},
  tabText: { fontSize: 15, fontWeight: '600' },
  badge: { marginLeft: 6, minWidth: 20, height: 20, borderRadius: 10, justifyContent: 'center', alignItems: 'center', paddingHorizontal: 6 },
  badgeText: { color: '#fff', fontSize: 12, fontWeight: '700' },
  markAllText: { fontSize: 14, fontWeight: '600' },
  listContent: { padding: 16, paddingBottom: 24 },
  row: { flexDirection: 'row', alignItems: 'flex-start', marginBottom: 12, padding: 14 },
  iconWrap: { width: 40, height: 40, borderRadius: 20, justifyContent: 'center', alignItems: 'center', marginRight: 12 },
  iconText: { fontSize: 18 },
  rowBody: { flex: 1, minWidth: 0 },
  rowTitle: { fontSize: 16, fontWeight: '700', marginBottom: 4 },
  rowMessage: { fontSize: 14, lineHeight: 20 },
  readLink: { marginTop: 8 },
  readLinkText: { fontSize: 13, fontWeight: '600' },
  rowTime: { fontSize: 12, marginLeft: 8 },
  empty: { marginTop: 24, padding: 24 },
  emptyTitle: { fontSize: 16, fontWeight: '600', marginBottom: 8 },
  emptyBody: { fontSize: 14 },
  footerHint: { fontSize: 13, textAlign: 'center', paddingBottom: 24 },
});
