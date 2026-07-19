import { api } from './client';
import { PaymentMethod } from './sales';

export enum CashRegisterStatus {
  Open = 0,
  Closed = 1,
}

export enum CashMovementType {
  Income = 0,
  Expense = 1,
}

export enum MovementReason {
  Sale = 0,
  CustomerPayment = 1,
  SupplierPayment = 2,
  CashWithdrawal = 3,
  PettyCash = 4,
  Other = 5,
}

export const CASH_REGISTER_STATUS_LABELS: Record<CashRegisterStatus, string> = {
  [CashRegisterStatus.Open]: 'Abierta',
  [CashRegisterStatus.Closed]: 'Cerrada',
};

export const CASH_MOVEMENT_TYPE_LABELS: Record<CashMovementType, string> = {
  [CashMovementType.Income]: 'Ingreso',
  [CashMovementType.Expense]: 'Egreso',
};

export const MOVEMENT_REASON_LABELS: Record<MovementReason, string> = {
  [MovementReason.Sale]: 'Venta',
  [MovementReason.CustomerPayment]: 'Cobro de cliente',
  [MovementReason.SupplierPayment]: 'Pago a proveedor',
  [MovementReason.CashWithdrawal]: 'Retiro de efectivo',
  [MovementReason.PettyCash]: 'Caja chica',
  [MovementReason.Other]: 'Otro',
};

export const PAYMENT_METHOD_LABELS: Record<PaymentMethod, string> = {
  [PaymentMethod.Cash]: 'Efectivo',
  [PaymentMethod.Card]: 'Tarjeta',
  [PaymentMethod.Transfer]: 'Transferencia',
  [PaymentMethod.YapePlin]: 'Yape/Plin',
  [PaymentMethod.Other]: 'Otro',
};

export interface CashMovement {
  id: string;
  type: CashMovementType;
  reason: MovementReason;
  paymentMethod: PaymentMethod;
  amount: number;
  description: string;
  saleId: string | null;
  createdBy: string | null;
  createdAt: string;
}

export interface CashRegister {
  id: string;
  tenantId: string;
  cashRegisterNumber: string;
  userId: string;
  warehouseId: string | null;
  openingAmount: number;
  openingDate: string;
  closingAmount: number | null;
  closingDate: string | null;
  expectedAmount: number | null;
  difference: number | null;
  status: CashRegisterStatus;
  notes: string | null;
  movements: CashMovement[];
}

export interface OpenCashRegisterPayload {
  openingAmount: number;
  warehouseId?: string | null;
  notes?: string | null;
}

export interface CloseCashRegisterPayload {
  closingAmount: number;
  notes?: string | null;
}

export interface RegisterCashMovementPayload {
  type: CashMovementType;
  reason: MovementReason;
  paymentMethod: PaymentMethod;
  amount: number;
  description: string;
  saleId?: string | null;
}

export const cashRegistersApi = {
  getOpen: () => api.get<CashRegister | null>('/api/CashRegisters/open'),
  getAll: (page = 1, pageSize = 10) =>
    api.get<{ total: number; page: number; pageSize: number; items: CashRegister[] }>(
      '/api/CashRegisters',
      { params: { page, pageSize } },
    ),
  getById: (id: string) => api.get<CashRegister>(`/api/CashRegisters/${id}`),
  open: (payload: OpenCashRegisterPayload) => api.post<CashRegister>('/api/CashRegisters/open', payload),
  close: (id: string, payload: CloseCashRegisterPayload) =>
    api.post<void>(`/api/CashRegisters/${id}/close`, payload),
  registerMovement: (id: string, payload: RegisterCashMovementPayload) =>
    api.post<void>(`/api/CashRegisters/${id}/movements`, payload),
};
