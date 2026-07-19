import { useNavigate, useParams } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { Table } from '../components/ui/Table';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import { useProducts } from '../hooks/useProducts';
import { useSuppliers } from '../hooks/useSuppliers';
import {
  usePurchase,
  useConfirmPurchase,
  useCancelPurchase,
} from '../hooks/usePurchases';
import {
  PurchaseStatus,
  VoucherType,
  Currency,
  PURCHASE_STATUS_LABELS,
  type Purchase,
} from '../api/purchases';

const STATUS_BADGE: Record<PurchaseStatus, string> = {
  [PurchaseStatus.Draft]: 'bg-gray-100 text-gray-700',
  [PurchaseStatus.Confirmed]: 'bg-green-100 text-green-700',
  [PurchaseStatus.Cancelled]: 'bg-red-100 text-red-700',
};

const VOUCHER_LABELS: Record<VoucherType, string> = {
  [VoucherType.Boleta]: 'Boleta',
  [VoucherType.Factura]: 'Factura',
  [VoucherType.NotaCredito]: 'Nota de Crédito',
  [VoucherType.Otro]: 'Otro',
};

export function PurchaseDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();

  const { data: productsData } = useProducts(1, 1000);
  const products = productsData?.items ?? [];
  const { data: suppliersData } = useSuppliers(1, 1000);
  const suppliers = suppliersData?.items ?? [];

  const { data: purchase, isLoading, isError, error } = usePurchase(id);
  const confirmMutation = useConfirmPurchase();
  const cancelMutation = useCancelPurchase();

  const supplierName = (sid: string) => suppliers.find((s) => s.id === sid)?.name ?? sid;
  const productName = (pid: string) => products.find((p) => p.id === pid)?.name ?? pid;

  const handleConfirm = async () => {
    if (!id) return;
    if (!window.confirm('¿Confirmar esta compra? Se registrará el ingreso de stock.')) return;
    try {
      await confirmMutation.mutateAsync(id);
    } catch (err: any) {
      window.alert(extractError(err));
    }
  };

  const handleCancel = async () => {
    if (!id) return;
    if (!window.confirm('¿Cancelar esta compra?')) return;
    try {
      await cancelMutation.mutateAsync(id);
    } catch (err: any) {
      window.alert(extractError(err));
    }
  };

  if (isLoading) return <LoadingSpinner />;
  if (isError) return <ErrorMessage message={extractError(error)} />;
  if (!purchase) return <ErrorMessage message="Compra no encontrada." />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">Compra {purchase.purchaseNumber}</h1>
        <span className={`px-3 py-1 rounded-full text-sm font-medium ${STATUS_BADGE[purchase.status]}`}>
          {PURCHASE_STATUS_LABELS[purchase.status]}
        </span>
      </div>

      <div className="bg-white rounded-lg shadow p-5 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 text-sm">
        <Field label="Proveedor" value={supplierName(purchase.supplierId)} />
        <Field label="Comprobante" value={VOUCHER_LABELS[purchase.voucherType]} />
        <Field label="Número" value={purchase.voucherNumber} />
        <Field label="Fecha" value={new Date(purchase.purchaseDate).toLocaleDateString('es-PE')} />
        <Field label="Moneda" value={purchase.currency === Currency.PEN ? 'PEN (S/)' : 'USD ($)'} />
        <Field label="Tipo de cambio" value={String(purchase.exchangeRate)} />
        <Field label="Observaciones" value={purchase.observations ?? '—'} />
      </div>

      <div className="bg-white rounded-lg shadow p-5">
        <h2 className="text-lg font-semibold text-gray-800 mb-3">Items</h2>
        <Table<Purchase['items'][number]>
          rowKey={(it) => it.id}
          columns={[
            { header: 'Producto', accessor: (it) => productName(it.productId) },
            { header: 'Cantidad', accessor: (it) => it.quantity },
            { header: 'Costo Unit.', accessor: (it) => `S/ ${it.unitCost.toFixed(2)}` },
            { header: 'Desc. %', accessor: (it) => `${it.discountPercentage}%` },
            { header: 'Subtotal', accessor: (it) => `S/ ${it.lineSubtotal.toFixed(2)}` },
          ]}
          data={purchase.items}
        />
      </div>

      <div className="bg-white rounded-lg shadow p-5 flex flex-col items-end gap-1 text-sm">
        <div className="flex justify-between w-64">
          <span className="text-gray-600">Subtotal:</span>
          <span className="font-medium">S/ {purchase.subtotal.toFixed(2)}</span>
        </div>
        <div className="flex justify-between w-64">
          <span className="text-gray-600">IGV (18%):</span>
          <span className="font-medium">S/ {purchase.tax.toFixed(2)}</span>
        </div>
        <div className="flex justify-between w-64 border-t pt-1">
          <span className="text-gray-800 font-semibold">Total:</span>
          <span className="font-bold text-lg">S/ {purchase.total.toFixed(2)}</span>
        </div>
      </div>

      <div className="flex gap-3">
        {purchase.status === PurchaseStatus.Draft && (
          <>
            <Button onClick={() => navigate(`/compras/${purchase.id}/editar`)}>Editar</Button>
            <Button onClick={handleConfirm} disabled={confirmMutation.isPending}>
              Confirmar
            </Button>
            <Button variant="danger" onClick={handleCancel} disabled={cancelMutation.isPending}>
              Cancelar
            </Button>
          </>
        )}
        {purchase.status === PurchaseStatus.Confirmed && (
          <Button variant="danger" onClick={handleCancel} disabled={cancelMutation.isPending}>
            Cancelar
          </Button>
        )}
        <Button variant="secondary" onClick={() => navigate('/compras')}>
          Volver
        </Button>
      </div>
    </div>
  );
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <div className="text-xs text-gray-500">{label}</div>
      <div className="font-medium text-gray-800">{value}</div>
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
