import { api } from './client';

export interface Category {
  id: string;
  tenantId: string;
  name: string;
  description: string | null;
  isActive: boolean;
}

export interface CategoryPayload {
  name: string;
  description?: string | null;
  isActive?: boolean;
}

export interface PagedResult<T> {
  total: number;
  page: number;
  pageSize: number;
  items: T[];
}

export const categoriesApi = {
  getAll: (page = 1, pageSize = 10) =>
    api.get<PagedResult<Category>>('/api/Categories', { params: { page, pageSize } }),

  getById: (id: string) => api.get<Category>(`/api/Categories/${id}`),

  create: (payload: CategoryPayload) => api.post<Category>('/api/Categories', payload),

  update: (id: string, payload: CategoryPayload) =>
    api.put<void>(`/api/Categories/${id}`, { id, ...payload }),

  remove: (id: string) => api.delete<void>(`/api/Categories/${id}`),
};
