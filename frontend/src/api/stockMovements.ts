import { api } from './client';

export enum StockMovementType {
  Entrada = 0,
  Salida = 1,
  Ajuste = 2,
}

export const STOCK_MOVEMENT_TYPE_LABELS: Record<StockMovementType, string> = {
  [StockMovementType.Entrada]: 'Entrada',
  [StockMovementType.Salida]: 'Salida',
  [StockMovementType.Ajuste]: 'Ajuste',
};

export interface StockMovement {
  id: string;
  tenantId: string;
  productId: string;
  type: StockMovementType;
  quantity: number;
  reason: string;
  previousStock: number;
  newStock: number;
  createdAt: string;
}

export interface StockMovementPayload {
  productId: string;
  type: StockMovementType;
  quantity: number;
  reason: string;
}

export interface PagedResult<T> {
  total: number;
  page: number;
  pageSize: number;
  items: T[];
}

export const stockMovementsApi = {
  getAll: (productId?: string, page = 1, pageSize = 10) =>
    api.get<PagedResult<StockMovement>>('/api/StockMovements', {
      params: { productId, page, pageSize },
    }),

  getById: (id: string) => api.get<StockMovement>(`/api/StockMovements/${id}`),

  create: (payload: StockMovementPayload) => api.post<StockMovement>('/api/StockMovements', payload),
};
