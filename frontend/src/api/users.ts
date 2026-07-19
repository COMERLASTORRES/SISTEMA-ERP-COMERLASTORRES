import { api } from './client';

export interface RoleSummary {
  id: string;
  name: string;
  isSystemRole: boolean;
}

export interface User {
  id: string;
  tenantId: string;
  email: string;
  fullName: string;
  isActive: boolean;
  roles: RoleSummary[];
}

export interface CreateUserPayload {
  email: string;
  password: string;
  fullName: string;
}

export interface UpdateUserPayload {
  id: string;
  fullName: string;
  isActive: boolean;
}

export interface AssignUserRolesPayload {
  roleIds: string[];
}

export const usersApi = {
  getAll: () => api.get<User[]>('/api/Users'),

  getById: (id: string) => api.get<User>(`/api/Users/${id}`),

  create: (payload: CreateUserPayload) => api.post<User>('/api/Users', payload),

  update: (id: string, payload: UpdateUserPayload) =>
    api.put<void>(`/api/Users/${id}`, { id, ...payload }),

  assignRoles: (id: string, roleIds: string[]) =>
    api.put<void>(`/api/Users/${id}/roles`, { roleIds }),
};
