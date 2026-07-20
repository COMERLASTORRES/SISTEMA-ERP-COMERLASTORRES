import { useState } from 'react';
import { Button } from '../components/ui/Button';
import { Table } from '../components/ui/Table';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import { RequirePermission } from '../components/RequirePermission';
import { PermissionCodes } from '../api/permissionCodes';
import { useSalesByPeriodReport } from '../hooks/useReports';
import { useCustomers } from '../hooks/useCustomers';
import {
  SaleStatus,
  PaymentStatus,
  SALE_STATUS_LABELS,
  PAYMENT_TYPE_LABELS,
} from '../api/sales';
import { REPORT_PAYMENT_STATUS_LABELS, type SalesByPeriodFilters } from '../api/reports';

const PAGE_SIZE = 10;

const STATUS_BADGE: Record<SaleStatus, string> = {
  [SaleStatus.Draft]: 'bg-gray-100 text-gray-700',
  [SaleStatus.Confirmed]: 'bg-green-100 text-gray-700',
  [SaleStatus.Cancelled]: 'bg-red-100 text-red-700',
};

function firstDayOfMonth(): string {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
}

function today(): string {
  return new Date().toISOString().slice(0, 10);
}

function formatMoney(value: number): string {
  return `S/ ${value.toFixed(2)}`;
}

export function SalesReportPage() {
  return (
    <RequirePermission
      codes={PermissionCodes.ReportsView}
      fallback={
        <div className="p-6 text-center text-gray-600">
          No tienes permiso para ver este módulo.
        </div>
      }
    >
      <SalesReportContent />
    </RequirePermission>
  );
}

function SalesReportContent() {
  // Filtros del formulario (lo que el usuario está editando).
  const [dateFrom, setDateFrom] = useState<string>(firstDayOfMonth());
  const [dateTo, setDateTo] = useState<string>(today());
  const [customerId, setCustomerId] = useState<string>('');
  const [paymentStatus, setPaymentStatus] = useState<string>('');
  // Por defecto "Confirmadas". "Todas" se representa con '' (no se envía documentStatus
  // al backend, así la tabla muestra ambos estados pero el resumen sigue solo Confirmed).
  const [documentStatus, setDocumentStatus] = useState<string>('confirmed');
  const [search, setSearch] = useState<string>('');
  const [page, setPage] = useState(1);

  // Filtros aplicados (los que realmente se envían al backend al pulsar "Generar reporte").
  const [applied, setApplied] = useState<SalesByPeriodFilters>({
    dateFrom: firstDayOfMonth(),
    dateTo: today(),
    page: 1,
    pageSize: PAGE_SIZE,
  });

  const { data: customersData } = useCustomers(1, 1000);
  const customers = customersData?.items ?? [];

  const { data, isLoading, isError, error } = useSalesByPeriodReport(applied);

  const items = data?.items ?? [];
  const summary = data?.summary;
  const totalCount = data?.totalCount ?? 0;
  const totalPages = data ? Math.max(1, Math.ceil(totalCount / PAGE_SIZE)) : 1;

  const generateReport = () => {
    setPage(1);
    setApplied({
      dateFrom: dateFrom || undefined,
      dateTo: dateTo || undefined,
      customerId: customerId || undefined,
      paymentStatus: paymentStatus === '' ? undefined : (Number(paymentStatus) as PaymentStatus),
      // "Todas" (value '') -> includeAllStatuses=true (tabla sin filtro de estado), sin
      // documentStatus. Estado específico -> documentStatus y sin includeAllStatuses.
      // Resumen siempre solo Confirmed en cualquier caso.
      documentStatus: documentStatus === '' ? undefined : (Number(documentStatus) as SaleStatus),
      includeAllStatuses: documentStatus === '' ? true : undefined,
      search: search.trim() || undefined,
      page: 1,
      pageSize: PAGE_SIZE,
    });
  };

  const handlePageChange = (next: number) => {
    setPage(next);
    setApplied((prev) => ({ ...prev, page: next }));
  };

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold text-gray-800">Reporte de Ventas por Período</h1>

      {/* Filtros */}
      <div className="bg-white rounded-lg shadow p-4 space-y-4">
        <div className="flex flex-wrap gap-4">
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Desde</label>
            <input
              type="date"
              className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={dateFrom}
              onChange={(e) => setDateFrom(e.target.value)}
            />
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Hasta</label>
            <input
              type="date"
              className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={dateTo}
              onChange={(e) => setDateTo(e.target.value)}
            />
          </div>
          <div className="flex flex-col gap-1 max-w-xs">
            <label className="text-sm font-medium text-gray-700">Cliente</label>
            <select
              className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={customerId}
              onChange={(e) => setCustomerId(e.target.value)}
            >
              <option value="">Todos</option>
              {customers.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </div>
          <div className="flex flex-col gap-1 max-w-xs">
            <label className="text-sm font-medium text-gray-700">Estado de Pago</label>
            <select
              className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={paymentStatus}
              onChange={(e) => setPaymentStatus(e.target.value)}
            >
              <option value="">Todos</option>
              <option value={PaymentStatus.Pending}>Pendiente</option>
              <option value={PaymentStatus.Partial}>Parcial</option>
              <option value={PaymentStatus.Paid}>Pagado</option>
            </select>
          </div>
          <div className="flex flex-col gap-1 max-w-xs">
            <label className="text-sm font-medium text-gray-700">Estado del Documento</label>
            <select
              className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={documentStatus}
              onChange={(e) => setDocumentStatus(e.target.value)}
            >
              <option value={SaleStatus.Confirmed}>Confirmadas</option>
              <option value={SaleStatus.Cancelled}>Canceladas</option>
              <option value="">Todas</option>
            </select>
          </div>
          <div className="flex flex-col gap-1 flex-1 min-w-[200px]">
            <label className="text-sm font-medium text-gray-700">Búsqueda (N° venta)</label>
            <input
              type="text"
              placeholder="Ej. VEN-000001"
              className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <div className="flex items-end">
            <Button onClick={generateReport}>Generar Reporte</Button>
          </div>
        </div>
      </div>

      {/* Tarjetas de resumen */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <SummaryCard label="Total de Ventas" value={formatMoney(summary?.totalAmount ?? 0)} />
        <SummaryCard label="Total IGV" value={formatMoney(summary?.totalTax ?? 0)} />
        <SummaryCard label="Subtotal" value={formatMoney(summary?.totalSubtotal ?? 0)} />
        <SummaryCard label="Documentos" value={String(summary?.totalRecords ?? 0)} />
      </div>

      {isLoading && <LoadingSpinner />}
      {isError && <ErrorMessage message={extractError(error)} />}

      {!isLoading && !isError && (
        <>
          <Table<(typeof items)[number]>
            rowKey={(r) => r.saleNumber}
            columns={[
              { header: 'Número', accessor: (r) => r.saleNumber },
              {
                header: 'Fecha',
                accessor: (r) => new Date(r.saleDate).toLocaleDateString('es-PE'),
              },
              { header: 'Cliente', accessor: (r) => r.customerName },
              { header: 'Vendedor', accessor: (r) => r.vendorName },
              {
                header: 'Tipo de Pago',
                accessor: (r) => PAYMENT_TYPE_LABELS[r.paymentType],
              },
              {
                header: 'Estado de Pago',
                accessor: (r) => REPORT_PAYMENT_STATUS_LABELS[r.paymentStatus],
              },
              {
                header: 'Estado',
                accessor: (r) => (
                  <span className={`px-2 py-1 rounded-full text-xs font-medium ${STATUS_BADGE[r.status]}`}>
                    {SALE_STATUS_LABELS[r.status]}
                  </span>
                ),
              },
              { header: 'Subtotal', accessor: (r) => formatMoney(r.subtotal) },
              { header: 'IGV', accessor: (r) => formatMoney(r.tax) },
              { header: 'Total', accessor: (r) => formatMoney(r.total) },
            ]}
            data={items}
          />

          <div className="flex items-center justify-between text-sm text-gray-600">
            <span>
              Total: {totalCount} | Página {page} de {totalPages}
            </span>
            <div className="flex gap-2">
              <Button
                variant="secondary"
                disabled={page <= 1}
                onClick={() => handlePageChange(Math.max(1, page - 1))}
              >
                Anterior
              </Button>
              <Button
                variant="secondary"
                disabled={page >= totalPages}
                onClick={() => handlePageChange(Math.min(totalPages, page + 1))}
              >
                Siguiente
              </Button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

function SummaryCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="bg-white rounded-lg shadow p-4">
      <div className="text-xs uppercase tracking-wider text-gray-500">{label}</div>
      <div className="mt-1 text-xl font-bold text-gray-800">{value}</div>
    </div>
  );
}

function extractError(err: any): string {
  if (err?.response?.data) {
    if (typeof err.response.data === 'string') return err.response.data;
    if (typeof err.response.data.message === 'string') return err.response.data.message;
  }
  return 'Ocurrió un error inesperado.';
}
