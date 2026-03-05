import { apiFetch } from './client';
import type { SubscriptionDto } from '../types/api';

export async function getSubscription(): Promise<SubscriptionDto> {
  return apiFetch<SubscriptionDto>('/api/subscription');
}
