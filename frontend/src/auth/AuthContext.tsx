import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react';
import { api, tokenStorage } from '../api/client';
import { permissionsApi } from '../api/permissions';
import type { AuthResponse, LoginRequest, User } from '../types';

interface AuthContextValue {
  user: User | null;
  isAuthenticated: boolean;
  permissions: string[];
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

function decodeUserFromToken(token: string): User | null {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return {
      id: payload.userId,
      email: payload.email,
      role: payload.role,
      tenantId: payload.tenantId,
    };
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [permissions, setPermissions] = useState<string[]>([]);

  const loadPermissions = useCallback(async () => {
    try {
      const response = await permissionsApi.getMine();
      setPermissions(response.data.map((p) => p.code));
    } catch {
      setPermissions([]);
    }
  }, []);

  useEffect(() => {
    const token = tokenStorage.get();
    if (!token) return;
    // Reconstruye el usuario mínimo a partir del token guardado y carga sus permisos.
    const decoded = decodeUserFromToken(token);
    if (!decoded) {
      tokenStorage.clear();
      return;
    }
    setUser(decoded);
    void loadPermissions();
  }, [loadPermissions]);

  const login = async (data: LoginRequest) => {
    const response = await api.post<AuthResponse>('/api/auth/login', data);
    const auth = response.data;
    tokenStorage.set(auth.token);
    setUser(decodeUserFromResponse(auth));
    await loadPermissions();
  };

  const logout = () => {
    tokenStorage.clear();
    setUser(null);
    setPermissions([]);
  };

  return (
    <AuthContext.Provider
      value={{ user, isAuthenticated: !!user, permissions, login, logout }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider');
  return ctx;
}
