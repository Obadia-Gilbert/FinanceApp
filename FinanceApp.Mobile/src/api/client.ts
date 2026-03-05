import * as SecureStore from 'expo-secure-store';

const API_BASE = process.env.EXPO_PUBLIC_API_URL ?? 'http://localhost:5279';

const TOKEN_KEY = 'auth_token';
const REFRESH_KEY = 'refresh_token';
const USER_KEY = 'auth_user';

export interface StoredUser {
  email: string;
  firstName: string;
  lastName: string;
}

export async function getStoredToken(): Promise<string | null> {
  try {
    return await SecureStore.getItemAsync(TOKEN_KEY);
  } catch {
    return null;
  }
}

export async function setStoredTokens(token: string, refreshToken: string, user: StoredUser): Promise<void> {
  await SecureStore.setItemAsync(TOKEN_KEY, token);
  await SecureStore.setItemAsync(REFRESH_KEY, refreshToken);
  await SecureStore.setItemAsync(USER_KEY, JSON.stringify(user));
}

export async function clearStoredAuth(): Promise<void> {
  await SecureStore.deleteItemAsync(TOKEN_KEY);
  await SecureStore.deleteItemAsync(REFRESH_KEY);
  await SecureStore.deleteItemAsync(USER_KEY);
}

export async function getStoredUser(): Promise<StoredUser | null> {
  const raw = await SecureStore.getItemAsync(USER_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as StoredUser;
  } catch {
    return null;
  }
}

async function refreshAuth(): Promise<boolean> {
  const refreshToken = await SecureStore.getItemAsync(REFRESH_KEY);
  if (!refreshToken) return false;
  const res = await fetch(`${API_BASE}/api/auth/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  });
  if (!res.ok) {
    await clearStoredAuth();
    return false;
  }
  const data = await res.json();
  await setStoredTokens(data.token, data.refreshToken, {
    email: data.email,
    firstName: data.firstName,
    lastName: data.lastName,
  });
  return true;
}

export async function apiFetch<T>(
  path: string,
  options: RequestInit & { skipAuth?: boolean } = {}
): Promise<T> {
  const { skipAuth, ...init } = options;
  const url = path.startsWith('http') ? path : `${API_BASE}${path}`;
  let token: string | null = skipAuth ? null : await getStoredToken();

  const doRequest = (authToken: string | null) => {
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...(init.headers as Record<string, string>),
    };
    if (authToken) headers['Authorization'] = `Bearer ${authToken}`;
    return fetch(url, { ...init, headers });
  };

  let res = await doRequest(token);

  if (res.status === 401 && !skipAuth && token) {
    const refreshed = await refreshAuth();
    if (refreshed) {
      token = await getStoredToken();
      res = await doRequest(token);
    }
  }

  if (!res.ok) {
    const text = await res.text();
    let message = res.statusText;
    try {
      const j = JSON.parse(text);
      if (j.message) message = j.message;
      else if (Array.isArray(j.errors)) message = j.errors.join(' ');
    } catch {
      if (text) message = text.slice(0, 200);
    }
    throw new ApiError(res.status, message);
  }

  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string
  ) {
    super(message);
    this.name = 'ApiError';
  }
}
