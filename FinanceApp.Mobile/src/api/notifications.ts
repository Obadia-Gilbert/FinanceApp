import { apiFetch } from './client';
import type { NotificationListResponse } from '../types/api';

export async function getUnreadCount(): Promise<number> {
  const res = await apiFetch<{ count: number }>('/api/notifications/unread-count');
  return res.count;
}

export async function getNotifications(page = 1, pageSize = 20): Promise<NotificationListResponse> {
  return apiFetch<NotificationListResponse>(`/api/notifications?page=${page}&pageSize=${pageSize}`);
}

export async function markNotificationRead(id: string): Promise<void> {
  await apiFetch(`/api/notifications/${id}/read`, { method: 'POST' });
}

export async function markAllNotificationsRead(): Promise<void> {
  await apiFetch('/api/notifications/mark-all-read', { method: 'POST' });
}
