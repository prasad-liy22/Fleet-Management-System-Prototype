import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react';
import { request, sessionExpiredEvent, setAccessToken } from '../api/client';
import type { CurrentUser, LoginResponse } from './types';

export const sessionKey = 'fleet.session';
interface Session { token: string; expiresAt: string }
interface AuthState {
  user: CurrentUser | null;
  status: 'loading' | 'anonymous' | 'authenticated';
  message: string;
  login(email: string, password: string): Promise<void>;
  logout(): Promise<void>;
}
const AuthContext = createContext<AuthState | null>(null);

function readSession(): Session | null {
  try {
    const data: unknown = JSON.parse(sessionStorage.getItem(sessionKey) ?? 'null');
    if (data && typeof data === 'object' && 'token' in data && 'expiresAt' in data &&
        typeof data.token === 'string' && typeof data.expiresAt === 'string' &&
        Date.parse(data.expiresAt) > Date.now())
      return { token: data.token, expiresAt: data.expiresAt };
  } catch { /* Malformed or blocked storage is treated as signed out. */ }
  return null;
}
function storeSession(session: Session | null) {
  try {
    if (session) sessionStorage.setItem(sessionKey, JSON.stringify(session));
    else sessionStorage.removeItem(sessionKey);
  } catch { /* The active session can still work in memory when storage is disabled. */ }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [status, setStatus] = useState<AuthState['status']>('loading');
  const [message, setMessage] = useState('');
  const [expiresAt, setExpiresAt] = useState<string | null>(null);

  const clear = useCallback((reason = '') => {
    setAccessToken(null);
    storeSession(null);
    setUser(null);
    setExpiresAt(null);
    setStatus('anonymous');
    setMessage(reason);
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    const session = readSession();
    if (!session) { clear(); return () => controller.abort(); }
    setAccessToken(session.token);
    request<CurrentUser>('/auth/me', { signal: controller.signal }).then(current => {
      if (controller.signal.aborted) return;
      setUser(current);
      setExpiresAt(session.expiresAt);
      setStatus('authenticated');
    }).catch(() => {
      if (!controller.signal.aborted) clear('Your session could not be verified. Please sign in again.');
    });
    return () => controller.abort();
  }, [clear]);

  useEffect(() => {
    const expired = () => clear('Your session has expired. Please sign in again.');
    window.addEventListener(sessionExpiredEvent, expired);
    return () => window.removeEventListener(sessionExpiredEvent, expired);
  }, [clear]);

  useEffect(() => {
    if (!expiresAt) return;
    const timer = window.setTimeout(() => clear('Your session has expired. Please sign in again.'),
      Math.max(0, Date.parse(expiresAt) - Date.now()));
    return () => window.clearTimeout(timer);
  }, [expiresAt, clear]);

  async function login(email: string, password: string) {
    const result = await request<LoginResponse>('/auth/login',
      { method: 'POST', anonymous: true, body: JSON.stringify({ email, password }) });
    setAccessToken(result.accessToken);
    storeSession({ token: result.accessToken, expiresAt: result.expiresAt });
    setUser(result.user);
    setExpiresAt(result.expiresAt);
    setMessage('');
    setStatus('authenticated');
  }

  async function logout() {
    try { await request<void>('/auth/logout', { method: 'POST' }); }
    finally { clear(); }
  }

  return <AuthContext.Provider value={{ user, status, message, login, logout }}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) throw new Error('AuthProvider is required.');
  return value;
}
