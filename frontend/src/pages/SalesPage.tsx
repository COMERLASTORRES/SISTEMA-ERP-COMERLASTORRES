import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { Table } from '../components/ui/Table';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import { useSales, useDeleteSale, useConfirmSale, useCancelSale } from '../hooks/useSales';
import { useCustomers } from '../hooks/useCustomers';
import {
  SaleStatus,
  SALE_STATUS_LABELS,
  PAYMENT_TYPE_LABELS,
  type Sale,
} from '../api/sales';

const PAGE_SIZE = 10;

const STATUS_BADGE: Record<SaleStatus, string> = {
  [SaleStatus.Draft]: 'bg-gray-100 text-gray-700',
  [SaleStatus.Confirmed]: 'bg-green-100 text-green-700',
  [SaleStatus.Cancelled]: 'bg-red-100 text-red-700',
};

export function SalesPage() {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState<string>('');

  const { data: customersData } = useCustomers(1, 1000);
  const customers = customersData?.items ?? [];

  const customerName = useMemo(() => {
    const map = new Map(customers.map((c) => [c.id, c.name]));
    return (id: string) => map.get(id) ?? id;
  }, [customers]);

  const {
    data,
    isLoading,
    isError,
    error,
  } = useSales(statusFilter === '' ? undefined : Number(statusFilter), undefined, page, PAGE_SIZE);

  const deleteMutation = useDeleteSale();
  const confirmMutation = useConfirmSale();
  const cancelMutation = useCancelSale();

  const sales = data?.items ?? [];

  const handleDelete = async (s: Sale) => {
    if (!window.confirm(`¿Eliminar la venta ${s.saleNumber}? (solo si está en borrador)`)) return;
    try {
      await deleteMutation.mutateAsync(s.id);
    } catch (err: any) {
      window.alert(extractError(err));
    }
  };

  const handleConfirm = async (s: Sale) => {
    if (!window.confirm(`¿Confirmar la venta ${s.saleNumber}? Se registrará la salida de stock.`)) return;
    try {
      await confirmMutation.mutateAsync(s.id);
    } catch (err: any) {
      window.alert(extractError(err));
    }
  };

  const handleCancel = async (s: Sale) => {
    if (!window.confirm(`¿Cancelar la venta ${s.saleNumber}?`)) return;
    const reason = window.prompt('Motivo de la cancelación (opcional):');
    if (reason === null) return; // el usuario canceló el prompt
    try {
      await cancelMutation.mutateAsync({ id: s.id, reason: reason.trim() || null });
    } catch (err: any) {
      window.alert(extractError(err));
    }
  };

  if (isLoading) return <LoadingSpinner />;
  if (isError) return <ErrorMessage message={extractError(error)} />;

  const totalPages = data ? Math.max(1, Math.ceil(data.total / PAGE_SIZE)) : 1;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">Ventas</h1>
        <Button onClick={() => navigate('/ventas/nueva')}>Nueva Venta</Button>
      </div>

      <div className="flex flex-wrap gap-4">
        <div className="flex flex-col gap-1 max-w-xs">
          <label className="text-sm font-medium text-gray-700">Estado</label>
          <select
            className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value);
              setPage(1);
            }}
          >
            <option value="">Todos</option>
            <option value={SaleStatus.Draft}>Borrador</option>
            <option value={SaleStatus.Confirmed}>Confirmado</option>
            <option value={SaleStatus.Cancelled}>Cancelado</option>
          </select>
        </div>
      </div>

      <Table<Sale>
        rowKey={(s) => s.id}
        columns={[
          { header: 'Número', accessor: (s) => s.saleNumber },
          { header: 'Cliente', accessor: (s) => customerName(s.customerId) },
          {
            header: 'Fecha',
            accessor: (s) => new Date(s.saleDate).toLocaleDateString('es-PE'),
          },
          { header: 'Pago', accessor: (s) => PAYMENT_TYPE_LABELS[s.paymentType] },
          {
            header: 'Total',
            accessor: (s) => `S/ ${s.total.toFixed(2)}`,
          },
          {
            header: 'Estado',
            accessor: (s) => (
              <span className={`px-2 py-1 rounded-full text-xs font-medium ${STATUS_BADGE[s.status]}`}>
                {SALE_STATUS_LABELS[s.status]}
              </span>
            ),
          },
          {
            header: 'Acciones',
            accessor: (s) => (
              <div className="flex flex-wrap gap-2">
                <Button variant="secondary" onClick={() => navigate(`/ventas/${s.id}`)}>
                  Ver
                </Button>
                {s.status === SaleStatus.Draft && (
                  <Button variant="secondary" onClick={() => navigate(`/ventas/${s.id}/editar`)}>
                    Editar
                  </Button>
                )}
                {s.status === SaleStatus.Draft && (
                  <Button onClick={() => handleConfirm(s)}>Confirmar</Button>
                )}
                {s.status !== SaleStatus.Cancelled && (
                  <Button variant="danger" onClick={() => handleCancel(s)}>
                    Cancelar
                  </Button>
                )}
                {s.status === SaleStatus.Draft && (
                  <Button variant="danger" onClick={() => handleDelete(s)}>
                    Eliminar
                  </Button>
                )}
              </div>
            ),
          },
        ]}
        data={sales}
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
