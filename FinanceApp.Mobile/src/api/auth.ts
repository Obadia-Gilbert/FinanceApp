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

export async function loginWithExternal(body: {
  provider: 'google' | 'facebook';
  idToken?: string;
  accessToken?: string;
}): Promise<LoginResponse> {
  const res = await apiFetch<LoginResponse>('/api/auth/external', {
    method: 'POST',
    body: JSON.stringify({
      provider: body.provider,
      idToken: body.idToken ?? null,
      accessToken: body.accessToken ?? null,
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

/**
 * Triggers a password-reset email for `email` if an account exists. Always
 * resolves on success (the API returns 204 No Content regardless of whether
 * the email is registered, to avoid account enumeration).
 */
export async function forgotPassword(email: string): Promise<void> {
  await apiFetch<void>('/api/auth/forgot-password', {
    method: 'POST',
    body: JSON.stringify({ email }),
    skipAuth: true,
  });
}

/**
 * Completes a password reset using the email + base64url-encoded token sent
 * via email. Currently used only by deep-link / web flows; exposed here so a
 * future in-app reset screen can call it directly.
 */
export async function resetPassword(body: {
  email: string;
  code: string;
  newPassword: string;
}): Promise<void> {
  await apiFetch<void>('/api/auth/reset-password', {
    method: 'POST',
    body: JSON.stringify(body),
    skipAuth: true,
  });
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
