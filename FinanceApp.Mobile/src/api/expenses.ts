import { apiFetch } from './client';
import { getCurrencyIndex } from '../utils/currency';
import type {
  ExpenseDto,
  CreateExpenseRequest,
  UpdateExpenseRequest,
  PagedResultDto,
} from '../types/api';

/** Current time as HH:mm:ss in local timezone (for appending to date-only when creating expense). */
function nowTimeString(): string {
  const d = new Date();
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}:${String(d.getSeconds()).padStart(2, '0')}`;
}

/** Build API payload: currency as enum index; expenseDate with current time if only date provided. */
function toCreateExpensePayload(body: CreateExpenseRequest) {
  const dateOnly = body.expenseDate.includes('T') ? body.expenseDate.split('T')[0]! : body.expenseDate;
  const expenseDate = body.expenseDate.includes('T') ? body.expenseDate : `${dateOnly}T${nowTimeString()}`;
  return {
    amount: body.amount,
    currency: typeof body.currency === 'number' ? body.currency : getCurrencyIndex(body.currency),
    expenseDate,
    categoryId: body.categoryId,
    description: body.description ?? null,
  };
}

export async function getExpenses(
  pageNumber = 1,
  pageSize = 20,
  categoryId?: string
): Promise<PagedResultDto<ExpenseDto>> {
  const params = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });
  if (categoryId) params.set('categoryId', categoryId);
  return apiFetch<PagedResultDto<ExpenseDto>>(`/api/expenses?${params}`);
}

export async function getExpense(id: string): Promise<ExpenseDto> {
  return apiFetch<ExpenseDto>(`/api/expenses/${id}`);
}

export async function createExpense(body: CreateExpenseRequest): Promise<ExpenseDto> {
  return apiFetch<ExpenseDto>('/api/expenses', {
    method: 'POST',
    body: JSON.stringify(toCreateExpensePayload(body)),
  });
}

/** Build API payload for update: currency as enum index. */
function toUpdateExpensePayload(body: UpdateExpenseRequest) {
  const dateStr = body.expenseDate.includes('T') ? body.expenseDate.split('T')[0]! : body.expenseDate;
  return {
    amount: body.amount,
    currency: typeof body.currency === 'number' ? body.currency : getCurrencyIndex(body.currency),
    expenseDate: dateStr,
    categoryId: body.categoryId,
    description: body.description ?? null,
  };
}

export async function updateExpense(id: string, body: UpdateExpenseRequest): Promise<ExpenseDto> {
  return apiFetch<ExpenseDto>(`/api/expenses/${id}`, {
    method: 'PUT',
    body: JSON.stringify(toUpdateExpensePayload(body)),
  });
}

export async function deleteExpense(id: string): Promise<void> {
  return apiFetch(`/api/expenses/${id}`, { method: 'DELETE' });
}
