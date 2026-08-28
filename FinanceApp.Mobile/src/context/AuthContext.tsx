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

  // The stored session lives in SecureStore, so it can only be read back
  // asynchronously: the provider starts "not ready" and flips once the read
  // resolves. Guarded so a provider unmounted mid-read doesn't set state.
  useEffect(() => {
    let active = true;
    void Promise.all([getStoredToken(), getStoredUser()]).then(([token, user]) => {
      if (!active) return;
      setState({
        isReady: true,
        isSignedIn: !!token && !!user,
        user: user ?? null,
      });
    });
    return () => {
      active = false;
    };
  }, []);

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
