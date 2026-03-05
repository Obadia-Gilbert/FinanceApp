import { apiFetch } from './client';
import type {
  RecurringTemplateDto,
  CreateRecurringTemplateRequest,
  UpdateRecurringTemplateRequest,
  PagedResultDto,
} from '../types/api';

export async function getRecurringTemplates(
  pageNumber = 1,
  pageSize = 20
): Promise<PagedResultDto<RecurringTemplateDto>> {
  const params = new URLSearchParams({ pageNumber: String(pageNumber), pageSize: String(pageSize) });
  return apiFetch<PagedResultDto<RecurringTemplateDto>>(`/api/recurring?${params}`);
}

export async function getRecurringTemplate(id: string): Promise<RecurringTemplateDto> {
  return apiFetch<RecurringTemplateDto>(`/api/recurring/${id}`);
}

export async function createRecurringTemplate(
  body: CreateRecurringTemplateRequest
): Promise<RecurringTemplateDto> {
  return apiFetch<RecurringTemplateDto>('/api/recurring', {
    method: 'POST',
    body: JSON.stringify(body),
  });
}

export async function updateRecurringTemplate(
  id: string,
  body: UpdateRecurringTemplateRequest
): Promise<RecurringTemplateDto> {
  return apiFetch<RecurringTemplateDto>(`/api/recurring/${id}`, {
    method: 'PUT',
    body: JSON.stringify(body),
  });
}

export async function deactivateRecurringTemplate(id: string): Promise<void> {
  return apiFetch(`/api/recurring/${id}/deactivate`, { method: 'POST' });
}

export async function deleteRecurringTemplate(id: string): Promise<void> {
  return apiFetch(`/api/recurring/${id}`, { method: 'DELETE' });
}
