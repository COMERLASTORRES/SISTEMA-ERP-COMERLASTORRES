import { api } from './client';

export interface PermissionRef {
  id: string;
  code: string;
  module: string;
  description: string;
}

export interface Role {
  id: string;
  tenantId: string;
  name: string;
  description: string | null;
  isSystemRole: boolean;
  permissions: PermissionRef[];
}

export interface RoleSummary {
  id: string;
  tenantId: string;
  name: string;
  description: string | null;
  isSystemRole: boolean;
  permissionCount: number;
}

export interface RolePayload {
  name: string;
  description?: string | null;
}

export interface AssignPermissionsPayload {
  permissionIds: string[];
}

export const rolesApi = {
  getAll: () => api.get<RoleSummary[]>('/api/Roles'),

  getById: (id: string) => api.get<Role>(`/api/Roles/${id}`),

  create: (payload: RolePayload) => api.post<Role>('/api/Roles', payload),

  update: (id: string, payload: RolePayload) =>
    api.put<void>(`/api/Roles/${id}`, { id, ...payload }),

  remove: (id: string) => api.delete<void>(`/api/Roles/${id}`),

  assignPermissions: (id: string, permissionIds: string[]) =>
    api.post<void>(`/api/Roles/${id}/permissions`, { permissionIds }),
};
