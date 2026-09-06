import { useState } from 'react';
import { Button } from '../components/ui/Button';
import { Table } from '../components/ui/Table';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import { RequirePermission } from '../components/RequirePermission';
import { PermissionCodes } from '../api/permissionCodes';
import { useValuedStockReport } from '../hooks/useReports';
import type { SalesByPeriodFilters } from '../api/reports';

const PAGE_SIZE = 10;

function formatMoney(value: number): string {
  return `S/ ${value.toLocaleString('es-PE', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;
}

export function ValuedStockReportPage() {
  return (
    <RequirePermission
      codes={PermissionCodes.ReportsView}
      fallback={
        <div className="p-6 text-center text-gray-600">
          No tienes permiso para ver este módulo.
        </div>
      }
    >
      <ValuedStockReportContent />
    </RequirePermission>
  );
}

export function ValuedStockReportContent() {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);

  const [applied, setApplied] = useState<SalesByPeriodFilters>({
    page: 1,
    pageSize: PAGE_SIZE,
  });

  const { data, isLoading, isError, error } = useValuedStockReport(applied);

  const items = data?.items ?? [];
  const summary = data?.summary;
  const totalCount = data?.totalCount ?? 0;
  const totalPages = data ? Math.max(1, Math.ceil(totalCount / PAGE_SIZE)) : 1;

  const generateReport = () => {
    setPage(1);
    setApplied({
      search: search || undefined,
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
      <h1 className="text-2xl font-bold text-gray-800">Stock Valorizado</h1>

      {/* Filtros */}
      <div className="bg-white rounded-lg shadow p-4 space-y-4">
        <div className="flex flex-wrap gap-4">
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Buscar</label>
            <input
              type="text"
              placeholder="Nombre o código..."
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
      <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
        <SummaryCard label="Valor total stock" value={formatMoney(summary?.totalAmount ?? 0)} variant="money" />
        <SummaryCard label="Productos" value={String(summary?.totalRecords ?? 0)} variant="product" />
      </div>

      {isLoading && <LoadingSpinner />}
      {isError && <ErrorMessage message={extractError(error)} />}

      {!isLoading && !isError && (
        <>
          <Table<(typeof items)[number]>
            rowKey={(r) => r.productId}
            columns={[
              { header: 'Producto', accessor: (r) => r.productName },
              { header: 'Stock', accessor: (r) => String(r.stock) },
              { header: 'Precio compra', accessor: (r) => formatMoney(r.purchasePrice) },
              { header: 'Valor total', accessor: (r) => formatMoney(r.totalValue) },
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