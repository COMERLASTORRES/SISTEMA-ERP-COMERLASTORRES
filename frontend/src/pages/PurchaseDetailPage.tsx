import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { Table } from '../components/ui/Table';
import { Modal } from '../components/ui/Modal';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import { RequirePermission } from '../components/RequirePermission';
import { PermissionCodes } from '../api/permissionCodes';
import { useProducts } from '../hooks/useProducts';
import { useSuppliers } from '../hooks/useSuppliers';
import { useOpenCashRegister } from '../hooks/useCashRegisters';
import {
  usePurchase,
  useConfirmPurchase,
  useCancelPurchase,
  useRegisterPurchasePayment,
} from '../hooks/usePurchases';
import {
  PurchaseStatus,
  VoucherType,
  Currency,
  PaymentType,
  PaymentMethod,
  PaymentStatus,
  PURCHASE_STATUS_LABELS,
  PAYMENT_TYPE_LABELS,
  PAYMENT_METHOD_LABELS,
  PURCHASE_PAYMENT_STATUS_LABELS,
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
  return (
    <RequirePermission
      codes={PermissionCodes.PurchasesView}
      fallback={
        <div className="p-6 text-center text-gray-600">
          No tienes permiso para ver este módulo.
        </div>
      }
    >
      <PurchaseDetailContent />
    </RequirePermission>
  );
}

function PurchaseDetailContent() {
  const { id } = useParams();
  const navigate = useNavigate();

  const { data: productsData } = useProducts(1, 1000);
  const products = productsData?.items ?? [];
  const { data: suppliersData } = useSuppliers(1, 1000);
  const suppliers = suppliersData?.items ?? [];

  const { data: purchase, isLoading, isError, error } = usePurchase(id);
  const confirmMutation = useConfirmPurchase();
  const cancelMutation = useCancelPurchase();
  const registerPaymentMutation = useRegisterPurchasePayment();
  const { data: openCashRegister } = useOpenCashRegister();

  const [paymentModalOpen, setPaymentModalOpen] = useState(false);
  const [selectedPaymentMethod, setSelectedPaymentMethod] = useState<PaymentMethod>(
    PaymentMethod.Cash,
  );

  // Para compras al contado, cancelar genera un ingreso de caja inverso, por lo que
  // requiere una caja abierta del usuario (igual que confirmar). Crédito no aplica.
  const cashBlockedForCancel =
    purchase?.paymentType === PaymentType.Cash && openCashRegister == null;

  // El pago de compras a crédito registra un egreso de caja, por lo que requiere
  // una caja abierta del usuario (mismo patrón que registrar cobro de ventas).
  const cashBlockedForPayment =
    purchase?.paymentType === PaymentType.Credit && openCashRegister == null;

  const canRegisterPayment =
    purchase?.status === PurchaseStatus.Confirmed &&
    purchase?.paymentType === PaymentType.Credit &&
    purchase?.paymentStatus === PaymentStatus.Pending;

  const [formError, setFormError] = useState('');

  const handleOpenPaymentModal = () => {
    if (!id || !canRegisterPayment) return;
    if (cashBlockedForPayment) return;
    setSelectedPaymentMethod(PaymentMethod.Cash);
    setPaymentModalOpen(true);
  };

  const handleConfirmPayment = async () => {
    if (!id) return;
    try {
      await registerPaymentMutation.mutateAsync({ id, paymentMethod: selectedPaymentMethod });
      setPaymentModalOpen(false);
    } catch (err: any) {
      setFormError(extractError(err));
    }
  };

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
    const reason = window.prompt('Motivo de la cancelación (opcional):');
    if (reason === null) return; // el usuario canceló el prompt
    try {
      await cancelMutation.mutateAsync({ id, reason: reason.trim() || null });
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
        <Field label="Tipo de pago" value={PAYMENT_TYPE_LABELS[purchase.paymentType]} />
        <Field
          label="Método de pago"
          value={
            purchase.paymentType === PaymentType.Cash
              ? purchase.paymentMethod != null
                ? PAYMENT_METHOD_LABELS[purchase.paymentMethod]
                : '—'
              : '—'
          }
        />
        <Field
          label="Días de crédito"
          value={purchase.paymentType === PaymentType.Credit ? `${purchase.creditDays ?? 0} días` : '—'}
        />
        <Field
          label="Fecha de vencimiento"
          value={
            purchase.paymentType === PaymentType.Credit && purchase.dueDate
              ? new Date(purchase.dueDate).toLocaleDateString('es-PE')
              : '—'
          }
        />
        <Field
          label="Estado de pago"
          value={PURCHASE_PAYMENT_STATUS_LABELS[purchase.paymentStatus]}
        />
        <Field label="Observaciones" value={purchase.observations ?? '—'} />
        {purchase.status === PurchaseStatus.Cancelled && (
          <>
            <Field label="Motivo de cancelación" value={purchase.cancellationReason ?? '—'} />
            <Field label="Cancelado por" value={purchase.cancelledBy ?? '—'} />
            <Field
              label="Fecha de cancelación"
              value={purchase.cancelledAt ? new Date(purchase.cancelledAt).toLocaleString('es-PE') : '—'}
            />
          </>
        )}
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
            <RequirePermission codes={PermissionCodes.PurchasesEdit}>
              <Button onClick={() => navigate(`/compras/${purchase.id}/editar`)}>Editar</Button>
            </RequirePermission>
            <RequirePermission codes={PermissionCodes.PurchasesConfirm}>
              <Button onClick={handleConfirm} disabled={confirmMutation.isPending}>
                Confirmar
              </Button>
            </RequirePermission>
            <RequirePermission codes={PermissionCodes.PurchasesCancel}>
              <Button
                variant="danger"
                onClick={handleCancel}
                disabled={cancelMutation.isPending || cashBlockedForCancel}
              >
                Cancelar
              </Button>
            </RequirePermission>
            {cashBlockedForCancel && (
              <div className="flex items-center gap-2 text-sm text-red-600">
                <span>Debe abrir una caja antes de cancelar compras al contado.</span>
                <Button variant="secondary" onClick={() => navigate('/caja')}>
                  Ir a Caja
                </Button>
              </div>
            )}
          </>
        )}
        {purchase.status === PurchaseStatus.Confirmed && (
          <>
            <RequirePermission codes={PermissionCodes.PurchasesRegisterPayment}>
              <Button
                onClick={handleOpenPaymentModal}
                disabled={registerPaymentMutation.isPending || cashBlockedForPayment}
              >
                Registrar Pago
              </Button>
            </RequirePermission>
            {canRegisterPayment && cashBlockedForPayment && (
              <div className="flex items-center gap-2 text-sm text-red-600">
                <span>Debe abrir una caja antes de registrar un pago.</span>
                <Button variant="secondary" onClick={() => navigate('/caja')}>
                  Ir a Caja
                </Button>
              </div>
            )}
            <RequirePermission codes={PermissionCodes.PurchasesCancel}>
              <Button
                variant="danger"
                onClick={handleCancel}
                disabled={cancelMutation.isPending || cashBlockedForCancel}
              >
                Cancelar
              </Button>
            </RequirePermission>
            {cashBlockedForCancel && (
              <div className="flex items-center gap-2 text-sm text-red-600">
                <span>Debe abrir una caja antes de cancelar compras al contado.</span>
                <Button variant="secondary" onClick={() => navigate('/caja')}>
                  Ir a Caja
                </Button>
              </div>
            )}
          </>
        )}
        <Button variant="secondary" onClick={() => navigate('/compras')}>
          Volver
        </Button>
      </div>

      <Modal open={paymentModalOpen} title="Registrar Pago" onClose={() => setPaymentModalOpen(false)}>
        <div className="space-y-4">
          <p className="text-sm text-gray-600">
            Seleccione el método de pago para el pago total de{' '}
            <strong>S/ {purchase.total.toFixed(2)}</strong>.
          </p>
          <div className="grid grid-cols-2 gap-2">
            {(
              [
                PaymentMethod.Cash,
                PaymentMethod.Card,
                PaymentMethod.Transfer,
                PaymentMethod.YapePlin,
                PaymentMethod.Other,
              ] as PaymentMethod[]
            ).map((m) => (
              <button
                key={m}
                type="button"
                onClick={() => setSelectedPaymentMethod(m)}
                className={`px-3 py-2 rounded-md text-sm font-medium border transition-colors ${
                  selectedPaymentMethod === m
                    ? 'border-blue-500 bg-blue-50 text-blue-700'
                    : 'border-gray-300 text-gray-700 hover:bg-gray-50'
                }`}
              >
                {PAYMENT_METHOD_LABELS[m]}
              </button>
            ))}
          </div>
          {formError && <ErrorMessage message={formError} />}
          <div className="flex justify-end gap-3">
            <Button variant="secondary" onClick={() => setPaymentModalOpen(false)}>
              Cancelar
            </Button>
            <Button onClick={handleConfirmPayment} disabled={registerPaymentMutation.isPending}>
              Confirmar Pago
            </Button>
          </div>
        </div>
      </Modal>
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
