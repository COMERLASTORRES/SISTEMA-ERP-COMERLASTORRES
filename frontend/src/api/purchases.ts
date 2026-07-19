import { api } from './client';

export enum PurchaseStatus {
  Draft = 0,
  Confirmed = 1,
  Cancelled = 2,
}

export enum VoucherType {
  Boleta = 0,
  Factura = 1,
  NotaCredito = 2,
  Otro = 3,
}

export enum Currency {
  PEN = 0,
  USD = 1,
}

export const PURCHASE_STATUS_LABELS: Record<PurchaseStatus, string> = {
  [PurchaseStatus.Draft]: 'Borrador',
  [PurchaseStatus.Confirmed]: 'Confirmado',
  [PurchaseStatus.Cancelled]: 'Cancelado',
};

export interface PurchaseItem {
  id: string;
  productId: string;
  quantity: number;
  unitCost: number;
  discountPercentage: number;
  lineSubtotal: number;
}

export interface Purchase {
  id: string;
  tenantId: string;
  purchaseNumber: string;
  supplierId: string;
  warehouseId: string | null;
  voucherType: VoucherType;
  voucherNumber: string;
  purchaseDate: string;
  currency: Currency;
  exchangeRate: number;
  observations: string | null;
  subtotal: number;
  tax: number;
  total: number;
  status: PurchaseStatus;
  createdBy: string | null;
  confirmedBy: string | null;
  confirmedAt: string | null;
  items: PurchaseItem[];
}

export interface PurchaseItemPayload {
  productId: string;
  quantity: number;
  unitCost: number;
  discountPercentage?: number;
}

export interface CreatePurchasePayload {
  supplierId: string;
  warehouseId?: string | null;
  voucherType: VoucherType;
  voucherNumber: string;
  purchaseDate: string;
  currency: Currency;
  exchangeRate?: number;
  observations?: string | null;
  items: PurchaseItemPayload[];
}

export interface UpdatePurchasePayload extends CreatePurchasePayload {
  id: string;
}

export interface PagedResult<T> {
  total: number;
  page: number;
  pageSize: number;
  items: T[];
}

export const purchasesApi = {
  getAll: (status?: PurchaseStatus, supplierId?: string, page = 1, pageSize = 10) =>
    api.get<PagedResult<Purchase>>('/api/Purchases', {
      params: { status, supplierId, page, pageSize },
    }),

  getById: (id: string) => api.get<Purchase>(`/api/Purchases/${id}`),

  create: (payload: CreatePurchasePayload) => api.post<Purchase>('/api/Purchases', payload),

  update: (id: string, payload: UpdatePurchasePayload) =>
    api.put<void>(`/api/Purchases/${id}`, payload),

  confirm: (id: string) => api.post<void>(`/api/Purchases/${id}/confirm`),

  cancel: (id: string) => api.post<void>(`/api/Purchases/${id}/cancel`),

  remove: (id: string) => api.delete<void>(`/api/Purchases/${id}`),
};
