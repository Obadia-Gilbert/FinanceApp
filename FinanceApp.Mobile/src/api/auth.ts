import { apiFetch, clearStoredAuth, setStoredTokens } from './client';
import type { LoginRequest, LoginResponse, RegisterRequest } from '../types/api';

export async function login(body: LoginRequest): Promise<LoginResponse> {
  const res = await apiFetch<LoginResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email: body.email, password: body.password }),
    skipAuth: true,
  });
  await setStoredTokens(res.token, res.refreshToken, {
    email: res.email,
    firstName: res.firstName,
    lastName: res.lastName,
  });
  return res;
}

export async function register(body: RegisterRequest): Promise<LoginResponse> {
  const res = await apiFetch<LoginResponse>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify({
      firstName: body.firstName,
      lastName: body.lastName,
      email: body.email,
      password: body.password,
    }),
    skipAuth: true,
  });
  await setStoredTokens(res.token, res.refreshToken, {
    email: res.email,
    firstName: res.firstName,
    lastName: res.lastName,
  });
  return res;
}

export async function logout(refreshToken: string): Promise<void> {
  try {
    await apiFetch('/api/auth/revoke', {
      method: 'POST',
      body: JSON.stringify({ refreshToken }),
    });
  } catch {
    // ignore
  } finally {
    await clearStoredAuth();
  }
}
