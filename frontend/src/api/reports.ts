import { api } from './client';
import type { SaleStatus, PaymentType } from './sales';
import { PaymentStatus } from './sales';

// Labels propios del reporte para el "Estado de Pago" (Pending/Partial/Paid).
export const REPORT_PAYMENT_STATUS_LABELS: Record<PaymentStatus, string> = {
  [PaymentStatus.Pending]: 'Pendiente',
  [PaymentStatus.Partial]: 'Parcial',
  [PaymentStatus.Paid]: 'Pagado',
};

export type SortDirection = 'Asc' | 'Desc';

/** Filtros del reporte de ventas por período. Todos opcionales salvo page/pageSize. */
export interface SalesByPeriodFilters {
  dateFrom?: string; // yyyy-MM-dd
  dateTo?: string; // yyyy-MM-dd
  customerId?: string;
  vendorId?: string;
  warehouseId?: string;
  paymentType?: PaymentType;
  paymentStatus?: PaymentStatus;
  documentStatus?: SaleStatus;
  /** true = tabla sin filtro de estado (todas). Tiene prioridad sobre documentStatus. */
  includeAllStatuses?: boolean;
  search?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: SortDirection;
}

export interface SalesReportItem {
  saleNumber: string;
  saleDate: string;
  customerName: string;
  vendorName: string;
  paymentType: PaymentType;
  paymentStatus: PaymentStatus;
  status: SaleStatus;
  subtotal: number;
  tax: number;
  total: number;
}

export interface ReportSummary {
  totalAmount: number;
  totalTax: number;
  totalSubtotal: number;
  totalRecords: number;
}

export interface PagedSalesReport {
  items: SalesReportItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  summary: ReportSummary;
}

export const reportsApi = {
  getSalesByPeriod: (filters: SalesByPeriodFilters = {}) =>
    api.get<PagedSalesReport>('/api/reports/sales/by-period', { params: filters }),
};
