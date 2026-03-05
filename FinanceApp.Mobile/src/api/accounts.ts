import { apiFetch } from './client';
import type { AccountDto, CreateAccountRequest, UpdateAccountRequest } from '../types/api';

export async function getAccounts(): Promise<AccountDto[]> {
  return apiFetch<AccountDto[]>('/api/accounts');
}

export async function getAccount(id: string): Promise<AccountDto> {
  return apiFetch<AccountDto>(`/api/accounts/${id}`);
}

export async function createAccount(body: CreateAccountRequest): Promise<AccountDto> {
  return apiFetch<AccountDto>('/api/accounts', {
    method: 'POST',
    body: JSON.stringify(body),
  });
}

export async function updateAccount(id: string, body: UpdateAccountRequest): Promise<AccountDto> {
  return apiFetch<AccountDto>(`/api/accounts/${id}`, {
    method: 'PUT',
    body: JSON.stringify(body),
  });
}

export async function deactivateAccount(id: string): Promise<void> {
  return apiFetch(`/api/accounts/${id}`, { method: 'DELETE' });
}
