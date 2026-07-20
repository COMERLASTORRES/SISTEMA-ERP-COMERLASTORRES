import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { Table } from '../components/ui/Table';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import { RequirePermission } from '../components/RequirePermission';
import { PermissionCodes } from '../api/permissionCodes';
import { useCashRegisters } from '../hooks/useCashRegisters';
import {
  CashRegisterStatus,
  CASH_REGISTER_STATUS_LABELS,
  type CashRegister,
} from '../api/cashRegisters';

const PAGE_SIZE = 10;

const STATUS_BADGE: Record<CashRegisterStatus, string> = {
  [CashRegisterStatus.Open]: 'bg-green-100 text-green-700',
  [CashRegisterStatus.Closed]: 'bg-gray-100 text-gray-700',
};

export function CashRegisterHistoryPage() {
  return (
    <RequirePermission
      codes={PermissionCodes.CashRegisterView}
      fallback={
        <div className="p-6 text-center text-gray-600">
          No tienes permiso para ver este módulo.
        </div>
      }
    >
      <CashRegisterHistoryContent />
    </RequirePermission>
  );
}

function CashRegisterHistoryContent() {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);

  const { data, isLoading, isError, error } = useCashRegisters(page, PAGE_SIZE);

  if (isLoading) return <LoadingSpinner />;
  if (isError) return <ErrorMessage message={extractError(error)} />;

  const registers = data?.items ?? [];
  const totalPages = data ? Math.max(1, Math.ceil(data.total / PAGE_SIZE)) : 1;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">Historial de Cajas</h1>
        <Button variant="secondary" onClick={() => navigate('/caja')}>
          Volver a Caja
        </Button>
      </div>

      <Table<CashRegister>
        rowKey={(c) => c.id}
        columns={[
          { header: 'Número', accessor: (c) => c.cashRegisterNumber },
          {
            header: 'Apertura',
            accessor: (c) => new Date(c.openingDate).toLocaleString('es-PE'),
          },
          { header: 'Monto apertura', accessor: (c) => `S/ ${c.openingAmount.toFixed(2)}` },
          {
            header: 'Esperado',
            accessor: (c) => (c.expectedAmount != null ? `S/ ${c.expectedAmount.toFixed(2)}` : '—'),
          },
          {
            header: 'Diferencia',
            accessor: (c) =>
              c.difference != null ? `S/ ${c.difference.toFixed(2)}` : '—',
          },
          {
            header: 'Estado',
            accessor: (c) => (
              <span
                className={`px-2 py-1 rounded-full text-xs font-medium ${STATUS_BADGE[c.status]}`}
              >
                {CASH_REGISTER_STATUS_LABELS[c.status]}
              </span>
            ),
          },
          {
            header: 'Acciones',
            accessor: (c) => (
              <Button variant="secondary" onClick={() => navigate(`/caja/${c.id}`)}>
                Ver
              </Button>
            ),
          },
        ]}
        data={registers}
      />

      {data && (
        <div className="flex items-center justify-between text-sm text-gray-600">
          <span>
            Total: {data.total} | Página {page} de {totalPages}
          </span>
          <div className="flex gap-2">
            <Button
              variant="secondary"
              disabled={page <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
            >
              Anterior
            </Button>
            <Button
              variant="secondary"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            >
              Siguiente
            </Button>
          </div>
        </div>
      )}
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
