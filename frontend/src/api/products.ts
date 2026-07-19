import { api } from './client';
import type { PagedResult } from './categories';

export interface Product {
  id: string;
  tenantId: string;
  code: string;
  name: string;
  barcode: string | null;
  purchasePrice: number;
  salePrice: number;
  stock: number;
  stockMinimum: number;
  categoryId: string | null;
  isActive: boolean;
  rowVersion: string;
}

export interface ProductPayload {
  code: string;
  name: string;
  barcode?: string | null;
  purchasePrice: number;
  salePrice: number;
  stockMinimum: number;
  categoryId?: string | null;
  isActive?: boolean;
}

export const productsApi = {
  getAll: (page = 1, pageSize = 10) =>
    api.get<PagedResult<Product>>('/api/Products', { params: { page, pageSize } }),

  getById: (id: string) => api.get<Product>(`/api/Products/${id}`),

  create: (payload: ProductPayload) => api.post<Product>('/api/Products', payload),

  update: (id: string, payload: ProductPayload & { id: string; rowVersion: string }) =>
    api.put<void>(`/api/Products/${id}`, payload),

  remove: (id: string) => api.delete<void>(`/api/Products/${id}`),
};
