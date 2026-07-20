import { useMemo, useState } from 'react';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Table } from '../components/ui/Table';
import { Modal } from '../components/ui/Modal';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import { RequirePermission } from '../components/RequirePermission';
import { PermissionCodes } from '../api/permissionCodes';
import { useProducts } from '../hooks/useProducts';
import {
  useStockMovements,
  useCreateStockMovement,
  type StockMovement,
  type StockMovementPayload,
} from '../hooks/useStockMovements';
import {
  StockMovementType,
  STOCK_MOVEMENT_TYPE_LABELS,
} from '../api/stockMovements';

const PAGE_SIZE = 10;

const TYPE_BADGE: Record<StockMovementType, string> = {
  [StockMovementType.Entrada]: 'bg-green-100 text-green-700',
  [StockMovementType.Salida]: 'bg-red-100 text-red-700',
  [StockMovementType.Ajuste]: 'bg-blue-100 text-blue-700',
};

export function StockMovementsPage() {
  return (
    <RequirePermission
      codes={PermissionCodes.StockMovementsView}
      fallback={
        <div className="p-6 text-center text-gray-600">
          No tienes permiso para ver este módulo.
        </div>
      }
    >
      <StockMovementsContent />
    </RequirePermission>
  );
}

function StockMovementsContent() {
  const [page, setPage] = useState(1);
  const [productFilter, setProductFilter] = useState<string>(''); // '' = todos
  const [modalOpen, setModalOpen] = useState(false);
  const [form, setForm] = useState<{
    productId: string;
    type: StockMovementType;
    quantity: string;
    reason: string;
  }>({
    productId: '',
    type: StockMovementType.Entrada,
    quantity: '',
    reason: '',
  });
  const [formError, setFormError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const { data: productsData } = useProducts(1, 1000);
  const products = productsData?.items ?? [];

  const {
    data,
    isLoading,
    isError,
    error,
  } = useStockMovements(productFilter || undefined, page, PAGE_SIZE);

  const createMutation = useCreateStockMovement();

  const movements = data?.items ?? [];

  const productName = useMemo(() => {
    const map = new Map(products.map((p) => [p.id, p.name]));
    return (id: string): string => map.get(id) ?? id;
  }, [products]);

  const openCreate = () => {
    setForm({ productId: products[0]?.id ?? '', type: StockMovementType.Entrada, quantity: '', reason: '' });
    setFormError('');
    setSuccessMessage('');
    setModalOpen(true);
  };

  const handleSubmit = async () => {
    setFormError('');
    setSuccessMessage('');
    if (!form.productId) {
      setFormError('Debe seleccionar un producto.');
      return;
    }
    const quantity = Number(form.quantity);
    if (!form.quantity.trim() || Number.isNaN(quantity) || quantity <= 0) {
      setFormError('La cantidad es requerida y debe ser mayor a 0.');
      return;
    }
    if (!form.reason.trim()) {
      setFormError('El motivo es requerido.');
      return;
    }
    const payload: StockMovementPayload = {
      productId: form.productId,
      type: form.type,
      quantity,
      reason: form.reason.trim(),
    };
    try {
      const { data: created } = await createMutation.mutateAsync(payload);
      setSuccessMessage(`Movimiento registrado. Nuevo stock: ${created.newStock}`);
      setModalOpen(false);
    } catch (err: any) {
      setFormError(extractError(err));
    }
  };

  if (isLoading) return <LoadingSpinner />;
  if (isError) return <ErrorMessage message={extractError(error)} />;

  const totalPages = data ? Math.max(1, Math.ceil(data.total / PAGE_SIZE)) : 1;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">Movimientos de Stock</h1>
        <RequirePermission codes={PermissionCodes.StockMovementsCreate}>
          <Button onClick={openCreate}>Nuevo Movimiento</Button>
        </RequirePermission>
      </div>

      <div className="flex flex-col gap-1 max-w-xs">
        <label className="text-sm font-medium text-gray-700">Producto</label>
        <select
          className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          value={productFilter}
          onChange={(e) => {
            setProductFilter(e.target.value);
            setPage(1);
          }}
        >
          <option value="">Todos los productos</option>
          {products.map((p) => (
            <option key={p.id} value={p.id}>
              {p.name}
            </option>
          ))}
        </select>
      </div>

      {successMessage && (
        <div className="rounded-md bg-green-50 border border-green-200 px-4 py-3 text-sm text-green-700">
          {successMessage}
        </div>
      )}

      <Table<StockMovement>
        rowKey={(m) => m.id}
        columns={[
          {
            header: 'Fecha',
            accessor: (m) =>
              new Date(m.createdAt).toLocaleString('es-PE', {
                day: '2-digit',
                month: '2-digit',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit',
              }),
          },
          { header: 'Producto', accessor: (m) => productName(m.productId) },
          {
            header: 'Tipo',
            accessor: (m) => (
              <span className={`px-2 py-1 rounded-full text-xs font-medium ${TYPE_BADGE[m.type]}`}>
                {STOCK_MOVEMENT_TYPE_LABELS[m.type]}
              </span>
            ),
          },
          { header: 'Cantidad', accessor: (m) => m.quantity },
          { header: 'Stock Anterior', accessor: (m) => m.previousStock },
          { header: 'Stock Nuevo', accessor: (m) => m.newStock },
          { header: 'Motivo', accessor: (m) => m.reason },
        ]}
        data={movements}
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

      <Modal open={modalOpen} title="Nuevo Movimiento" onClose={() => setModalOpen(false)}>
        <div className="space-y-4">
          {formError && <ErrorMessage message={formError} />}

          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Producto</label>
            <select
              className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={form.productId}
              onChange={(e) => setForm((f) => ({ ...f, productId: e.target.value }))}
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
            <label className="text-sm font-medium text-gray-700">Tipo</label>
            <select
              className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={form.type}
              onChange={(e) =>
                setForm((f) => ({ ...f, type: Number(e.target.value) as StockMovementType }))
              }
            >
              {Object.values(StockMovementType)
                .filter((v) => typeof v === 'number')
                .map((v) => (
                  <option key={v} value={v}>
                    {STOCK_MOVEMENT_TYPE_LABELS[v as StockMovementType]}
                  </option>
                ))}
            </select>
          </div>

          <Input
            label="Cantidad"
            type="number"
            value={form.quantity}
            onChange={(e) => setForm((f) => ({ ...f, quantity: e.target.value }))}
          />

          <Input
            label="Motivo"
            value={form.reason}
            onChange={(e) => setForm((f) => ({ ...f, reason: e.target.value }))}
          />

          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={() => setModalOpen(false)}>
              Cancelar
            </Button>
            <Button onClick={handleSubmit} disabled={createMutation.isPending}>
              Registrar
            </Button>
          </div>
        </div>
      </Modal>
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
