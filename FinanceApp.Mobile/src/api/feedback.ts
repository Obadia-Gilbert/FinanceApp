import { apiFetch } from './client';
import type { FeedbackDto, CreateFeedbackRequest, PagedResultDto } from '../types/api';

export async function getMyFeedback(
  pageNumber = 1,
  pageSize = 20
): Promise<PagedResultDto<FeedbackDto>> {
  const params = new URLSearchParams({ pageNumber: String(pageNumber), pageSize: String(pageSize) });
  return apiFetch<PagedResultDto<FeedbackDto>>(`/api/feedback?${params}`);
}

export async function getFeedback(id: string): Promise<FeedbackDto> {
  return apiFetch<FeedbackDto>(`/api/feedback/${id}`);
}

export async function createFeedback(body: CreateFeedbackRequest): Promise<FeedbackDto> {
  return apiFetch<FeedbackDto>('/api/feedback', {
    method: 'POST',
    body: JSON.stringify(body),
  });
}
