import React, { createContext, useCallback, useContext, useEffect, useState } from 'react';
import * as SecureStore from 'expo-secure-store';
import {
  clearStoredAuth,
  getStoredToken,
  getStoredUser,
  type StoredUser,
} from '../api/client';
import { logout as apiLogout } from '../api/auth';

interface AuthState {
  isReady: boolean;
  isSignedIn: boolean;
  user: StoredUser | null;
}

interface AuthContextValue extends AuthState {
  signOut: () => Promise<void>;
  setUser: (user: StoredUser | null) => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

const REFRESH_KEY = 'refresh_token';

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<AuthState>({
    isReady: false,
    isSignedIn: false,
    user: null,
  });

  const loadStored = useCallback(async () => {
    const token = await getStoredToken();
    const user = await getStoredUser();
    setState({
      isReady: true,
      isSignedIn: !!token && !!user,
      user: user ?? null,
    });
  }, []);

  useEffect(() => {
    loadStored();
  }, [loadStored]);

  const signOut = useCallback(async () => {
    try {
      const refreshToken = await SecureStore.getItemAsync(REFRESH_KEY);
      if (refreshToken) await apiLogout(refreshToken);
    } catch {
      // ignore
    } finally {
      await clearStoredAuth();
      setState((s) => ({ ...s, isSignedIn: false, user: null }));
    }
  }, []);

  const setUser = useCallback((user: StoredUser | null) => {
    setState((s) => ({ ...s, user, isSignedIn: !!user }));
  }, []);

  const value: AuthContextValue = {
    ...state,
    signOut,
    setUser,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
