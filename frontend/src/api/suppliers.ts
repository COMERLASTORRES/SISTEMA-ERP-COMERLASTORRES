import { api } from './client';
import { DocumentType, DOCUMENT_TYPE_LABELS, DOCUMENT_HINTS } from './customers';

export interface Supplier {
  id: string;
  tenantId: string;
  documentType: DocumentType;
  documentNumber: string;
  name: string;
  email: string | null;
  phone: string | null;
  address: string | null;
  contactPerson: string | null;
  paymentTermDays: number;
  isActive: boolean;
}

export interface SupplierPayload {
  documentType: DocumentType;
  documentNumber: string;
  name: string;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
  contactPerson?: string | null;
  paymentTermDays?: number;
  isActive?: boolean;
}

export interface PagedResult<T> {
  total: number;
  page: number;
  pageSize: number;
  items: T[];
}

export const suppliersApi = {
  getAll: (page = 1, pageSize = 10) =>
    api.get<PagedResult<Supplier>>('/api/Suppliers', { params: { page, pageSize } }),

  getById: (id: string) => api.get<Supplier>(`/api/Suppliers/${id}`),

  create: (payload: SupplierPayload) => api.post<Supplier>('/api/Suppliers', payload),

  update: (id: string, payload: SupplierPayload) =>
    api.put<void>(`/api/Suppliers/${id}`, { id, ...payload }),

  remove: (id: string) => api.delete<void>(`/api/Suppliers/${id}`),
};

export { DOCUMENT_TYPE_LABELS, DOCUMENT_HINTS, DocumentType };
