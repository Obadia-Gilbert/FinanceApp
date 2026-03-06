import { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  RefreshControl,
  ActivityIndicator,
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

export default function NotificationsScreen() {
  const { colors } = useTheme();
  const queryClient = useQueryClient();
  const [filter, setFilter] = useState<'all' | 'unread'>('all');

  const { data, isLoading, refetch, isRefetching } = useQuery({
    queryKey: ['notifications'],
    queryFn: () => getNotifications(1, 50),
  });

  const list = data?.items ?? [];
  const unreadCount = list.filter((n) => !n.isRead).length;
  const filtered = filter === 'unread' ? list.filter((n) => !n.isRead) : list;

  const markReadMutation = useMutation({
    mutationFn: markNotificationRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['notificationsUnreadCount'] });
    },
  });

  const markAllMutation = useMutation({
    mutationFn: markAllNotificationsRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['notificationsUnreadCount'] });
    },
  });

  function iconBgForType(type: string): string {
    const t = (type || '').toLowerCase();
    if (t.includes('budget')) return `${colors.warning}25`;
    if (t.includes('subscription') || t.includes('renew')) return `${colors.info}25`;
    if (t.includes('goal') || t.includes('savings')) return `${colors.success}25`;
    if (t.includes('balance') || t.includes('low')) return `${colors.danger}25`;
    return `${colors.text.subtle}25`;
  }

  return (
    <View style={[styles.container, { backgroundColor: colors.bg.alt }]}>
      <View style={[styles.tabRow, { borderBottomColor: colors.border }]}>
        <View style={styles.tabs}>
          <TouchableOpacity
            style={[styles.tab, filter === 'all' && { borderBottomWidth: 2, borderBottomColor: colors.brand }]}
            onPress={() => setFilter('all')}
          >
            <Text style={[styles.tabText, { color: filter === 'all' ? colors.brand : colors.text.muted }]}>All</Text>
            {unreadCount > 0 && (
              <View style={[styles.badge, { backgroundColor: colors.brand }]}>
                <Text style={styles.badgeText}>{unreadCount}</Text>
              </View>
            )}
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.tab, filter === 'unread' && { borderBottomWidth: 2, borderBottomColor: colors.brand }]}
            onPress={() => setFilter('unread')}
          >
            <Text style={[styles.tabText, { color: filter === 'unread' ? colors.brand : colors.text.muted }]}>Unread</Text>
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
          <RefreshControl refreshing={isRefetching && !isLoading} onRefresh={() => refetch()} tintColor={colors.brand} />
        }
        ListEmptyComponent={
          isLoading ? (
            <View style={styles.loadingWrap}>
              <ActivityIndicator size="large" color={colors.brand} />
            </View>
          ) : (
            <View style={styles.emptyWrap}>
              <Text style={{ fontSize: 48, marginBottom: 12 }}>🔔</Text>
              <Text style={[styles.emptyTitle, { color: colors.text.primary }]}>
                {filter === 'unread' ? "You're all caught up!" : 'No notifications'}
              </Text>
              <Text style={[styles.emptyBody, { color: colors.text.muted }]}>
                {filter === 'unread' ? 'All notifications have been read.' : 'No notifications from the last 30 days.'}
              </Text>
            </View>
          )
        }
        renderItem={({ item }) => (
          <NotificationRow
            item={item}
            colors={colors}
            iconBg={iconBgForType(item.type)}
            onMarkRead={() => markReadMutation.mutate(item.id)}
          />
        )}
      />
    </View>
  );
}

function NotificationRow({
  item,
  colors,
  iconBg,
  onMarkRead,
}: {
  item: NotificationItemDto;
  colors: ReturnType<typeof useTheme>['colors'];
  iconBg: string;
  onMarkRead: () => void;
}) {
  const icon = iconForType(item.type);
  const timeStr = relativeTime(item.createdAt);

  return (
    <Card style={[styles.row, !item.isRead && { borderLeftWidth: 3, borderLeftColor: colors.brand }]}>
      <View style={[styles.iconWrap, { backgroundColor: iconBg }]}>
        <Text style={styles.iconText}>{icon}</Text>
      </View>
      <View style={styles.rowBody}>
        <View style={styles.rowTitleRow}>
          <Text style={[styles.rowTitle, { color: colors.text.primary }]} numberOfLines={1}>{item.title}</Text>
          <Text style={[styles.rowTime, { color: colors.text.subtle }]}>{timeStr}</Text>
        </View>
        <Text style={[styles.rowMessage, { color: colors.text.body }]}>{item.message}</Text>
        {!item.isRead && (
          <TouchableOpacity onPress={onMarkRead} style={styles.readLink} activeOpacity={0.7}>
            <Text style={[styles.readLinkText, { color: colors.brand }]}>Mark as read</Text>
          </TouchableOpacity>
        )}
      </View>
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
    paddingBottom: 0,
    borderBottomWidth: 1,
  },
  tabs: { flexDirection: 'row', alignItems: 'center' },
  tab: { flexDirection: 'row', alignItems: 'center', marginRight: 24, paddingVertical: 10 },
  tabText: { fontSize: 15, fontWeight: '600' },
  badge: { marginLeft: 6, minWidth: 20, height: 20, borderRadius: 10, justifyContent: 'center', alignItems: 'center', paddingHorizontal: 6 },
  badgeText: { color: '#fff', fontSize: 12, fontWeight: '700' },
  markAllText: { fontSize: 14, fontWeight: '600' },
  listContent: { padding: 16, paddingBottom: 24 },
  loadingWrap: { padding: 40, alignItems: 'center' },
  emptyWrap: { paddingTop: 60, alignItems: 'center', paddingHorizontal: 24 },
  emptyTitle: { fontSize: 18, fontWeight: '600', marginBottom: 8 },
  emptyBody: { fontSize: 14, textAlign: 'center' },
  row: { flexDirection: 'row', alignItems: 'flex-start', marginBottom: 12, padding: 14 },
  iconWrap: { width: 40, height: 40, borderRadius: 12, justifyContent: 'center', alignItems: 'center', marginRight: 12 },
  iconText: { fontSize: 18 },
  rowBody: { flex: 1, minWidth: 0 },
  rowTitleRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 4 },
  rowTitle: { fontSize: 15, fontWeight: '700', flex: 1 },
  rowMessage: { fontSize: 14, lineHeight: 20 },
  readLink: { marginTop: 8 },
  readLinkText: { fontSize: 13, fontWeight: '600' },
  rowTime: { fontSize: 11, marginLeft: 8 },
});
