import { useState } from 'react';
import { Button } from '../components/ui/Button';
import { Table } from '../components/ui/Table';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import { RequirePermission } from '../components/RequirePermission';
import { PermissionCodes } from '../api/permissionCodes';
import { useKardexReport } from '../hooks/useReports';
import { useProducts } from '../hooks/useProducts';
import type { SalesByPeriodFilters } from '../api/reports';

const PAGE_SIZE = 20;

function today(): string {
  return new Date().toISOString().slice(0, 10);
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('es-PE');
}

export function KardexReportPage() {
  return (
    <RequirePermission
      codes={PermissionCodes.ReportsView}
      fallback={
        <div className="p-6 text-center text-gray-600">
          No tienes permiso para ver este módulo.
        </div>
      }
    >
      <KardexReportContent />
    </RequirePermission>
  );
}

export function KardexReportContent() {
  const [productId, setProductId] = useState<string>('');
  const [dateFrom, setDateFrom] = useState<string>('');
  const [dateTo, setDateTo] = useState<string>(today());
  const [page, setPage] = useState(1);

  const { data: productsData } = useProducts(1, 1000);
  const products = productsData?.items ?? [];

  const [applied, setApplied] = useState<SalesByPeriodFilters>({
    dateTo: today(),
    page: 1,
    pageSize: PAGE_SIZE,
  });

  const { data, isLoading, isError, error } = useKardexReport(productId, applied);

  const items = data?.items ?? [];
  const summary = data?.summary;
  const totalCount = data?.totalCount ?? 0;
  const totalPages = data ? Math.max(1, Math.ceil(totalCount / PAGE_SIZE)) : 1;

  const generateReport = () => {
    if (!productId) return;
    setPage(1);
    setApplied({
      dateFrom: dateFrom || undefined,
      dateTo: dateTo || undefined,
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
      <h1 className="text-2xl font-bold text-gray-800">Kardex</h1>

      {/* Filtros */}
      <div className="bg-white rounded-lg shadow p-4 space-y-4">
        <div className="flex flex-wrap gap-4">
          <div className="flex flex-col gap-1 max-w-xs">
            <label className="text-sm font-medium text-gray-700">Producto</label>
            <select
              className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={productId}
              onChange={(e) => setProductId(e.target.value)}
            >
              <option value="">Seleccione un producto...</option>
              {products.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name}
                </option>
              ))}
            </select>
          </div>
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
          <div className="flex items-end">
            <Button onClick={generateReport} disabled={!productId}>
              Generar Reporte
            </Button>
          </div>
        </div>
      </div>

      {/* Tarjetas de resumen */}
      <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
        <SummaryCard label="Movimientos" value={String(summary?.totalRecords ?? 0)} variant="product" />
      </div>

      {isLoading && <LoadingSpinner />}
      {isError && <ErrorMessage message={extractError(error)} />}

      {!isLoading && !isError && !productId && (
        <div className="p-6 text-center text-gray-500">
          Seleccione un producto y haga clic en "Generar Reporte" para ver el kardex.
        </div>
      )}

      {!isLoading && !isError && productId && (
        <>
          <Table<(typeof items)[number]>
            rowKey={(r) => r.createdAt}
            columns={[
              { header: 'Fecha', accessor: (r) => formatDate(r.createdAt) },
              { header: 'Tipo', accessor: (r) => r.type },
              { header: 'Cantidad', accessor: (r) => String(r.quantity) },
              { header: 'Stock Anterior', accessor: (r) => String(r.previousStock) },
              { header: 'Stock Nuevo', accessor: (r) => String(r.newStock) },
              { header: 'Motivo', accessor: (r) => r.reason },
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

function SummaryCard({
  label,
  value,
  variant = 'default',
}: {
  label: string;
  value: string;
  variant?: 'money' | 'people' | 'product' | 'default';
}) {
  const borderColors = {
    money: 'border-l-blue-500',
    people: 'border-l-green-500',
    product: 'border-l-orange-500',
    default: 'border-l-gray-400',
  };
  return (
    <div className={`bg-white rounded-lg shadow-sm border-l-4 ${borderColors[variant]} p-4`}>
      <div className="text-xs font-medium text-gray-500">{label}</div>
      <div className="mt-1 text-lg font-bold text-gray-800">{value}</div>
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