import { apiFetch } from './client';
import type {
  TransactionDto,
  CreateTransactionRequest,
  CreateTransferRequest,
  UpdateTransactionRequest,
  PagedResultDto,
} from '../types/api';

export async function getTransactions(
  pageNumber = 1,
  pageSize = 20,
  accountId?: string,
  type?: string
): Promise<PagedResultDto<TransactionDto>> {
  const params = new URLSearchParams({ pageNumber: String(pageNumber), pageSize: String(pageSize) });
  if (accountId) params.set('accountId', accountId);
  if (type) params.set('type', type);
  return apiFetch<PagedResultDto<TransactionDto>>(`/api/transactions?${params}`);
}

export async function getTransaction(id: string): Promise<TransactionDto> {
  return apiFetch<TransactionDto>(`/api/transactions/${id}`);
}

export async function createTransaction(body: CreateTransactionRequest): Promise<TransactionDto> {
  return apiFetch<TransactionDto>('/api/transactions', {
    method: 'POST',
    body: JSON.stringify({
      ...body,
      date: body.date.split('T')[0],
    }),
  });
}

export async function createTransfer(body: CreateTransferRequest): Promise<{ from: TransactionDto; to: TransactionDto }> {
  return apiFetch<{ from: TransactionDto; to: TransactionDto }>('/api/transactions/transfer', {
    method: 'POST',
    body: JSON.stringify({
      ...body,
      date: body.date.split('T')[0],
    }),
  });
}

export async function updateTransaction(id: string, body: UpdateTransactionRequest): Promise<TransactionDto> {
  return apiFetch<TransactionDto>(`/api/transactions/${id}`, {
    method: 'PUT',
    body: JSON.stringify({
      ...body,
      date: body.date.split('T')[0],
    }),
  });
}

export async function deleteTransaction(id: string): Promise<void> {
  return apiFetch(`/api/transactions/${id}`, { method: 'DELETE' });
}
