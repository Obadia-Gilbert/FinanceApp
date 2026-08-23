import { apiFetch } from './client';
import type { AccountAuthStatus, DeleteAccountRequest, ProfileDto, UpdateProfileRequest } from '../types/api';

export async function getProfile(): Promise<ProfileDto> {
  return apiFetch<ProfileDto>('/api/profile');
}

export async function updateProfile(body: UpdateProfileRequest): Promise<ProfileDto> {
  return apiFetch<ProfileDto>('/api/profile', {
    method: 'PUT',
    body: JSON.stringify(body),
  });
}

export async function getAccountDeletionStatus(): Promise<AccountAuthStatus> {
  return apiFetch<AccountAuthStatus>('/api/profile/deletion-status');
}

export async function deleteAccount(body: DeleteAccountRequest): Promise<void> {
  await apiFetch<void>('/api/profile', {
    method: 'DELETE',
    body: JSON.stringify(body),
  });
}
