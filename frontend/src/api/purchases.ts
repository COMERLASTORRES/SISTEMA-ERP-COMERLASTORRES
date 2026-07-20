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

export enum PaymentType {
  Cash = 0,
  Credit = 1,
}

export enum PaymentMethod {
  Cash = 0,
  Card = 1,
  Transfer = 2,
  YapePlin = 3,
  Other = 4,
}

export enum PaymentStatus {
  Pending = 0,
  Partial = 1,
  Paid = 2,
}

export const PURCHASE_STATUS_LABELS: Record<PurchaseStatus, string> = {
  [PurchaseStatus.Draft]: 'Borrador',
  [PurchaseStatus.Confirmed]: 'Confirmado',
  [PurchaseStatus.Cancelled]: 'Cancelado',
};

export const PAYMENT_TYPE_LABELS: Record<PaymentType, string> = {
  [PaymentType.Cash]: 'Contado',
  [PaymentType.Credit]: 'Crédito',
};

export const PAYMENT_METHOD_LABELS: Record<PaymentMethod, string> = {
  [PaymentMethod.Cash]: 'Efectivo',
  [PaymentMethod.Card]: 'Tarjeta',
  [PaymentMethod.Transfer]: 'Transferencia',
  [PaymentMethod.YapePlin]: 'Yape/Plin',
  [PaymentMethod.Other]: 'Otro',
};

export const PURCHASE_PAYMENT_STATUS_LABELS: Record<PaymentStatus, string> = {
  [PaymentStatus.Pending]: 'Pendiente',
  [PaymentStatus.Partial]: 'Parcial',
  [PaymentStatus.Paid]: 'Pagado',
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
  paymentType: PaymentType;
  paymentMethod: PaymentMethod | null;
  creditDays: number | null;
  dueDate: string | null;
  paymentStatus: PaymentStatus;
  observations: string | null;
  subtotal: number;
  tax: number;
  total: number;
  status: PurchaseStatus;
  createdBy: string | null;
  confirmedBy: string | null;
  confirmedAt: string | null;
  cancelledBy: string | null;
  cancelledAt: string | null;
  cancellationReason: string | null;
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
  paymentType: PaymentType;
  paymentMethod?: PaymentMethod | null;
  creditDays?: number | null;
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
  getAll: (
    status?: PurchaseStatus,
    supplierId?: string,
    paymentType?: PaymentType,
    page = 1,
    pageSize = 10,
  ) =>
    api.get<PagedResult<Purchase>>('/api/Purchases', {
      params: { status, supplierId, paymentType, page, pageSize },
    }),

  getById: (id: string) => api.get<Purchase>(`/api/Purchases/${id}`),

  create: (payload: CreatePurchasePayload) => api.post<Purchase>('/api/Purchases', payload),

  update: (id: string, payload: UpdatePurchasePayload) =>
    api.put<void>(`/api/Purchases/${id}`, payload),

  confirm: (id: string) => api.post<void>(`/api/Purchases/${id}/confirm`),

  cancel: (id: string, reason?: string | null) =>
    api.post<void>(`/api/Purchases/${id}/cancel`, reason ? { reason } : {}),

  registerPayment: (id: string, paymentMethod: PaymentMethod) =>
    api.post<void>(`/api/Purchases/${id}/register-payment`, { paymentMethod }),

  remove: (id: string) => api.delete<void>(`/api/Purchases/${id}`),
};
