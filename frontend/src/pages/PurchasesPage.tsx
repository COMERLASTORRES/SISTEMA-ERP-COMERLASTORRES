import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { Table } from '../components/ui/Table';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import { RequirePermission } from '../components/RequirePermission';
import { PermissionCodes } from '../api/permissionCodes';
import { usePurchases, useDeletePurchase, useConfirmPurchase, useCancelPurchase } from '../hooks/usePurchases';
import { useSuppliers } from '../hooks/useSuppliers';
import { PurchaseStatus, PaymentType, PURCHASE_STATUS_LABELS, PAYMENT_TYPE_LABELS, type Purchase } from '../api/purchases';

const PAGE_SIZE = 10;

const STATUS_BADGE: Record<PurchaseStatus, string> = {
  [PurchaseStatus.Draft]: 'bg-gray-100 text-gray-700',
  [PurchaseStatus.Confirmed]: 'bg-green-100 text-green-700',
  [PurchaseStatus.Cancelled]: 'bg-red-100 text-red-700',
};

export function PurchasesPage() {
  return (
    <RequirePermission
      codes={PermissionCodes.PurchasesView}
      fallback={
        <div className="p-6 text-center text-gray-600">
          No tienes permiso para ver este módulo.
        </div>
      }
    >
      <PurchasesContent />
    </RequirePermission>
  );
}

function PurchasesContent() {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [supplierFilter, setSupplierFilter] = useState<string>('');
  const [paymentTypeFilter, setPaymentTypeFilter] = useState<string>('');

  const { data: suppliersData } = useSuppliers(1, 1000);
  const suppliers = suppliersData?.items ?? [];

  const supplierName = useMemo(() => {
    const map = new Map(suppliers.map((s) => [s.id, s.name]));
    return (id: string) => map.get(id) ?? id;
  }, [suppliers]);

  const {
    data,
    isLoading,
    isError,
    error,
  } = usePurchases(
    statusFilter === '' ? undefined : Number(statusFilter),
    supplierFilter || undefined,
    paymentTypeFilter === '' ? undefined : Number(paymentTypeFilter),
    page,
    PAGE_SIZE,
  );

  const deleteMutation = useDeletePurchase();
  const confirmMutation = useConfirmPurchase();
  const cancelMutation = useCancelPurchase();

  const purchases = data?.items ?? [];

  const handleDelete = async (p: Purchase) => {
    if (!window.confirm(`¿Eliminar la compra ${p.purchaseNumber}? (solo si está en borrador)`)) return;
    try {
      await deleteMutation.mutateAsync(p.id);
    } catch (err: any) {
      window.alert(extractError(err));
    }
  };

  const handleConfirm = async (p: Purchase) => {
    if (!window.confirm(`¿Confirmar la compra ${p.purchaseNumber}? Se registrará el ingreso de stock.`)) return;
    try {
      await confirmMutation.mutateAsync(p.id);
    } catch (err: any) {
      window.alert(extractError(err));
    }
  };

  const handleCancel = async (p: Purchase) => {
    if (!window.confirm(`¿Cancelar la compra ${p.purchaseNumber}?`)) return;
    const reason = window.prompt('Motivo de la cancelación (opcional):');
    if (reason === null) return; // el usuario canceló el prompt
    try {
      await cancelMutation.mutateAsync({ id: p.id, reason: reason.trim() || null });
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
        <h1 className="text-2xl font-bold text-gray-800">Compras</h1>
        <RequirePermission
          codes={PermissionCodes.PurchasesCreate}
          fallback={
            <span className="text-sm text-gray-500">
              No tienes permiso para crear compras.
            </span>
          }
        >
          <Button onClick={() => navigate('/compras/nueva')}>Nueva Compra</Button>
        </RequirePermission>
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
            <option value={PurchaseStatus.Draft}>Borrador</option>
            <option value={PurchaseStatus.Confirmed}>Confirmado</option>
            <option value={PurchaseStatus.Cancelled}>Cancelado</option>
          </select>
        </div>

        <div className="flex flex-col gap-1 max-w-xs">
          <label className="text-sm font-medium text-gray-700">Proveedor</label>
          <select
            className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={supplierFilter}
            onChange={(e) => {
              setSupplierFilter(e.target.value);
              setPage(1);
            }}
          >
            <option value="">Todos</option>
            {suppliers.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
        </div>

        <div className="flex flex-col gap-1 max-w-xs">
          <label className="text-sm font-medium text-gray-700">Tipo de Pago</label>
          <select
            className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={paymentTypeFilter}
            onChange={(e) => {
              setPaymentTypeFilter(e.target.value);
              setPage(1);
            }}
          >
            <option value="">Todos</option>
            <option value={PaymentType.Cash}>{PAYMENT_TYPE_LABELS[PaymentType.Cash]}</option>
            <option value={PaymentType.Credit}>{PAYMENT_TYPE_LABELS[PaymentType.Credit]}</option>
          </select>
        </div>
      </div>

      <Table<Purchase>
        rowKey={(p) => p.id}
        columns={[
          { header: 'Número', accessor: (p) => p.purchaseNumber },
          { header: 'Proveedor', accessor: (p) => supplierName(p.supplierId) },
          {
            header: 'Fecha',
            accessor: (p) =>
              new Date(p.purchaseDate).toLocaleDateString('es-PE'),
          },
          {
            header: 'Total',
            accessor: (p) => `S/ ${p.total.toFixed(2)}`,
          },
          {
            header: 'Estado',
            accessor: (p) => (
              <span className={`px-2 py-1 rounded-full text-xs font-medium ${STATUS_BADGE[p.status]}`}>
                {PURCHASE_STATUS_LABELS[p.status]}
              </span>
            ),
          },
          {
            header: 'Tipo de Pago',
            accessor: (p) => PAYMENT_TYPE_LABELS[p.paymentType],
          },
          {
            header: 'Acciones',
            accessor: (p) => (
              <div className="flex flex-wrap gap-2">
                <Button variant="secondary" onClick={() => navigate(`/compras/${p.id}`)}>
                  Ver
                </Button>
                {p.status === PurchaseStatus.Draft && (
                  <RequirePermission codes={PermissionCodes.PurchasesEdit}>
                    <Button variant="secondary" onClick={() => navigate(`/compras/${p.id}/editar`)}>
                      Editar
                    </Button>
                  </RequirePermission>
                )}
                {p.status === PurchaseStatus.Draft && (
                  <RequirePermission codes={PermissionCodes.PurchasesConfirm}>
                    <Button onClick={() => handleConfirm(p)}>Confirmar</Button>
                  </RequirePermission>
                )}
                {p.status !== PurchaseStatus.Cancelled && (
                  <RequirePermission codes={PermissionCodes.PurchasesCancel}>
                    <Button variant="danger" onClick={() => handleCancel(p)}>
                      Cancelar
                    </Button>
                  </RequirePermission>
                )}
                {p.status === PurchaseStatus.Draft && (
                  <RequirePermission codes={PermissionCodes.PurchasesDelete}>
                    <Button variant="danger" onClick={() => handleDelete(p)}>
                      Eliminar
                    </Button>
                  </RequirePermission>
                )}
              </div>
            ),
          },
        ]}
        data={purchases}
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
