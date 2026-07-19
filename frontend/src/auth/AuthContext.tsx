import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { api, tokenStorage } from '../api/client';
import type { AuthResponse, LoginRequest, User } from '../types';

interface AuthContextValue {
  user: User | null;
  isAuthenticated: boolean;
  login: (data: LoginRequest) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function decodeUserFromResponse(auth: AuthResponse): User {
  return {
    id: auth.userId,
    email: auth.email,
    role: auth.role,
    tenantId: auth.tenantId,
  };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);

  useEffect(() => {
    const token = tokenStorage.get();
    if (!token) return;
    // Reconstruye el usuario mínimo a partir del token guardado.
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      setUser({
        id: payload.userId,
        email: payload.email,
        role: payload.role,
        tenantId: payload.tenantId,
      });
    } catch {
      tokenStorage.clear();
    }
  }, []);

  const login = async (data: LoginRequest) => {
    const response = await api.post<AuthResponse>('/api/auth/login', data);
    const auth = response.data;
    tokenStorage.set(auth.token);
    setUser(decodeUserFromResponse(auth));
  };

  const logout = () => {
    tokenStorage.clear();
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider');
  return ctx;
}
