import { apiFetch } from './client';
import type {
  IncomeDto,
  CreateIncomeRequest,
  UpdateIncomeRequest,
  PagedResultDto,
} from '../types/api';

export async function getIncomes(
  pageNumber = 1,
  pageSize = 20,
  accountId?: string
): Promise<PagedResultDto<IncomeDto>> {
  const params = new URLSearchParams({ pageNumber: String(pageNumber), pageSize: String(pageSize) });
  if (accountId) params.set('accountId', accountId);
  return apiFetch<PagedResultDto<IncomeDto>>(`/api/income?${params}`);
}

export async function getIncome(id: string): Promise<IncomeDto> {
  return apiFetch<IncomeDto>(`/api/income/${id}`);
}

export async function createIncome(body: CreateIncomeRequest): Promise<IncomeDto> {
  return apiFetch<IncomeDto>('/api/income', {
    method: 'POST',
    body: JSON.stringify({
      ...body,
      incomeDate: body.incomeDate.split('T')[0],
      accountId: body.accountId && body.accountId !== '' ? body.accountId : null,
    }),
  });
}

export async function updateIncome(id: string, body: UpdateIncomeRequest): Promise<IncomeDto> {
  return apiFetch<IncomeDto>(`/api/income/${id}`, {
    method: 'PUT',
    body: JSON.stringify({
      ...body,
      incomeDate: body.incomeDate.split('T')[0],
      accountId: body.accountId && body.accountId !== '' ? body.accountId : null,
    }),
  });
}

export async function deleteIncome(id: string): Promise<void> {
  return apiFetch(`/api/income/${id}`, { method: 'DELETE' });
}
