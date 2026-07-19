import { api } from './client';

export interface Permission {
  id: string;
  code: string;
  module: string;
  description: string;
}

export const permissionsApi = {
  // Catálogo completo de permisos disponibles (global), para poblar los checkboxes.
  getAll: () => api.get<Permission[]>('/api/Permissions'),
};
