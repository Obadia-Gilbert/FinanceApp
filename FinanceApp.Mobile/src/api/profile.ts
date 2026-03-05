import { apiFetch } from './client';
import type { ProfileDto, UpdateProfileRequest } from '../types/api';

export async function getProfile(): Promise<ProfileDto> {
  return apiFetch<ProfileDto>('/api/profile');
}

export async function updateProfile(body: UpdateProfileRequest): Promise<ProfileDto> {
  return apiFetch<ProfileDto>('/api/profile', {
    method: 'PUT',
    body: JSON.stringify(body),
  });
}
