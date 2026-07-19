import { api } from './client';

export enum DocumentType {
  DNI = 0,
  RUC = 1,
  CE = 2,
  SinDocumento = 3,
}

export enum CustomerType {
  Regular = 0,
  Mayorista = 1,
  Corporativo = 2,
}

export const DOCUMENT_TYPE_LABELS: Record<DocumentType, string> = {
  [DocumentType.DNI]: 'DNI',
  [DocumentType.RUC]: 'RUC',
  [DocumentType.CE]: 'CE',
  [DocumentType.SinDocumento]: 'Sin Documento',
};

export const CUSTOMER_TYPE_LABELS: Record<CustomerType, string> = {
  [CustomerType.Regular]: 'Regular',
  [CustomerType.Mayorista]: 'Mayorista',
  [CustomerType.Corporativo]: 'Corporativo',
};

export const DOCUMENT_HINTS: Record<DocumentType, string> = {
  [DocumentType.DNI]: '8 dígitos',
  [DocumentType.RUC]: '11 dígitos empezando en 10 o 20',
  [DocumentType.CE]: 'Alfanumérico, máximo 12 caracteres',
  [DocumentType.SinDocumento]: 'Sin validación',
};

export interface Customer {
  id: string;
  tenantId: string;
  documentType: DocumentType;
  documentNumber: string;
  name: string;
  email: string | null;
  phone: string | null;
  address: string | null;
  customerType: CustomerType;
  creditLimit: number;
  isActive: boolean;
}

export interface CustomerPayload {
  documentType: DocumentType;
  documentNumber: string;
  name: string;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
  customerType: CustomerType;
  creditLimit?: number;
  isActive?: boolean;
}

export interface PagedResult<T> {
  total: number;
  page: number;
  pageSize: number;
  items: T[];
}

export const customersApi = {
  getAll: (page = 1, pageSize = 10) =>
    api.get<PagedResult<Customer>>('/api/Customers', { params: { page, pageSize } }),

  getById: (id: string) => api.get<Customer>(`/api/Customers/${id}`),

  create: (payload: CustomerPayload) => api.post<Customer>('/api/Customers', payload),

  update: (id: string, payload: CustomerPayload) =>
    api.put<void>(`/api/Customers/${id}`, { id, ...payload }),

  remove: (id: string) => api.delete<void>(`/api/Customers/${id}`),
};
