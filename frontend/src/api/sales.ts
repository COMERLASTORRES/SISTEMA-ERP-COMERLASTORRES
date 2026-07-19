import { api } from './client';

export enum SaleStatus {
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

export const SALE_STATUS_LABELS: Record<SaleStatus, string> = {
  [SaleStatus.Draft]: 'Borrador',
  [SaleStatus.Confirmed]: 'Confirmado',
  [SaleStatus.Cancelled]: 'Cancelado',
};

export const VOUCHER_LABELS: Record<VoucherType, string> = {
  [VoucherType.Boleta]: 'Boleta',
  [VoucherType.Factura]: 'Factura',
  [VoucherType.NotaCredito]: 'Nota de Crédito',
  [VoucherType.Otro]: 'Otro',
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

export interface SaleItem {
  id: string;
  productId: string;
  quantity: number;
  unitPrice: number;
  discountPercentage: number;
  taxPercentage: number;
  lineSubtotal: number;
  lineTax: number;
  lineTotal: number;
}

export interface Sale {
  id: string;
  tenantId: string;
  saleNumber: string;
  customerId: string;
  warehouseId: string | null;
  voucherType: VoucherType;
  voucherNumber: string | null;
  saleDate: string;
  currency: Currency;
  exchangeRate: number;
  paymentType: PaymentType;
  paymentMethod: PaymentMethod | null;
  creditDays: number | null;
  dueDate: string | null;
  paymentStatus: PaymentStatus;
  subtotal: number;
  tax: number;
  total: number;
  status: SaleStatus;
  createdBy: string | null;
  confirmedBy: string | null;
  confirmedAt: string | null;
  observations: string | null;
  items: SaleItem[];
}

export interface SaleItemPayload {
  productId: string;
  quantity: number;
  unitPrice: number;
  discountPercentage?: number;
  taxPercentage?: number;
}

export interface CreateSalePayload {
  customerId: string;
  warehouseId?: string | null;
  voucherType: VoucherType;
  voucherNumber?: string | null;
  saleDate: string;
  currency: Currency;
  exchangeRate?: number;
  paymentType: PaymentType;
  paymentMethod?: PaymentMethod | null;
  creditDays?: number | null;
  observations?: string | null;
  items: SaleItemPayload[];
}

export interface UpdateSalePayload extends CreateSalePayload {
  id: string;
}

export interface StockValidationError {
  productId: string;
  productName: string | null;
  requestedQuantity: number;
  availableStock: number;
}

export interface StockValidationResponse {
  isValid: boolean;
  errors: StockValidationError[];
}

export interface ValidateStockItemPayload {
  productId: string;
  quantity: number;
}

export interface PagedResult<T> {
  total: number;
  page: number;
  pageSize: number;
  items: T[];
}

export const salesApi = {
  getAll: (status?: SaleStatus, customerId?: string, page = 1, pageSize = 10) =>
    api.get<PagedResult<Sale>>('/api/Sales', {
      params: { status, customerId, page, pageSize },
    }),

  getById: (id: string) => api.get<Sale>(`/api/Sales/${id}`),

  create: (payload: CreateSalePayload) => api.post<Sale>('/api/Sales', payload),

  update: (id: string, payload: UpdateSalePayload) =>
    api.put<void>(`/api/Sales/${id}`, payload),

  validateStock: (items: ValidateStockItemPayload[]) =>
    api.post<StockValidationResponse>('/api/Sales/validate-stock', { items }),

  confirm: (id: string) => api.post<void>(`/api/Sales/${id}/confirm`),

  cancel: (id: string) => api.post<void>(`/api/Sales/${id}/cancel`),

  remove: (id: string) => api.delete<void>(`/api/Sales/${id}`),
};
