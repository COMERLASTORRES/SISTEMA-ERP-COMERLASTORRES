import { useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { Modal } from '../components/ui/Modal';
import { Table } from '../components/ui/Table';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import { RequirePermission } from '../components/RequirePermission';
import { PermissionCodes } from '../api/permissionCodes';
import { useProducts } from '../hooks/useProducts';
import { useCustomers } from '../hooks/useCustomers';
import { useOpenCashRegister } from '../hooks/useCashRegisters';
import {
  useSale,
  useConfirmSale,
  useCancelSale,
  useRegisterSalePayment,
  useValidateSaleStock,
} from '../hooks/useSales';
import {
  SaleStatus,
  VoucherType,
  Currency,
  PaymentType,
  PaymentMethod,
  PaymentStatus,
  SALE_STATUS_LABELS,
  VOUCHER_LABELS,
  PAYMENT_TYPE_LABELS,
  PAYMENT_METHOD_LABELS,
  type Sale,
  type StockValidationError,
} from '../api/sales';

const STATUS_BADGE: Record<SaleStatus, string> = {
  [SaleStatus.Draft]: 'bg-gray-100 text-gray-700',
  [SaleStatus.Confirmed]: 'bg-green-100 text-green-700',
  [SaleStatus.Cancelled]: 'bg-red-100 text-red-700',
};

const PAYMENT_STATUS_BADGE: Record<PaymentStatus, string> = {
  [PaymentStatus.Pending]: 'bg-yellow-100 text-yellow-700',
  [PaymentStatus.Partial]: 'bg-orange-100 text-orange-700',
  [PaymentStatus.Paid]: 'bg-green-100 text-green-700',
};

const VOUCHER_LABELS_MAP: Record<VoucherType, string> = VOUCHER_LABELS;

export function SaleDetailPage() {
  return (
    <RequirePermission
      codes={PermissionCodes.SalesView}
      fallback={
        <div className="p-6 text-center text-gray-600">
          No tienes permiso para ver este módulo.
        </div>
      }
    >
      <SaleDetailContent />
    </RequirePermission>
  );
}

function SaleDetailContent() {
  const { id } = useParams();
  const navigate = useNavigate();

  const { data: productsData } = useProducts(1, 1000);
  const products = productsData?.items ?? [];
  const { data: customersData } = useCustomers(1, 1000);
  const customers = customersData?.items ?? [];

  const { data: sale, isLoading, isError, error } = useSale(id);
  const confirmMutation = useConfirmSale();
  const cancelMutation = useCancelSale();
  const registerPaymentMutation = useRegisterSalePayment();
  const validateStockMutation = useValidateSaleStock();
  const { data: openCashRegister } = useOpenCashRegister();

  // Para ventas al contado, cancelar genera un egreso de caja inverso, por lo que
  // requiere una caja abierta del usuario (igual que confirmar). Crédito no aplica.
  const cashBlockedForCancel =
    sale?.paymentType === PaymentType.Cash && openCashRegister == null;

  // El cobro de ventas a crédito registra un ingreso de caja, por lo que requiere
  // una caja abierta del usuario (mismo patrón que confirmar/cancelar al contado).
  const cashBlockedForPayment =
    sale?.paymentType === PaymentType.Credit && openCashRegister == null;

  const canRegisterPayment =
    sale?.status === SaleStatus.Confirmed &&
    sale?.paymentType === PaymentType.Credit &&
    sale?.paymentStatus === PaymentStatus.Pending;

  const [formError, setFormError] = useState('');
  const [stockErrors, setStockErrors] = useState<StockValidationError[]>([]);
  const [paymentModalOpen, setPaymentModalOpen] = useState(false);
  const [selectedPaymentMethod, setSelectedPaymentMethod] = useState<PaymentMethod>(
    PaymentMethod.Cash,
  );

  const paymentLabel = useMemo(() => {
    if (!sale) return '';
    if (sale.paymentType === PaymentType.Credit) return `Crédito (${sale.creditDays} días)`;
    if (sale.paymentType === PaymentType.Cash && sale.paymentMethod != null) {
      return `Contado - ${PAYMENT_METHOD_LABELS[sale.paymentMethod]}`;
    }
    return PAYMENT_TYPE_LABELS[sale.paymentType];
  }, [sale]);

  const customerName = (cid: string) => customers.find((c) => c.id === cid)?.name ?? cid;
  const productName = (pid: string) => products.find((p) => p.id === pid)?.name ?? pid;

  const runStockValidation = async (): Promise<boolean> => {
    if (!sale) return false;
    const payload = sale.items
      .filter((it) => it.productId)
      .map((it) => ({ productId: it.productId, quantity: it.quantity }));
    try {
      const result = await validateStockMutation.mutateAsync(payload);
      setStockErrors(result.errors ?? []);
      return result.isValid;
    } catch (err: any) {
      setFormError(extractError(err));
      return false;
    }
  };

  const handleConfirm = async () => {
    if (!id) return;
    // Validación de stock antes de confirmar.
    const stockOk = await runStockValidation();
    if (!stockOk) return;
    if (!window.confirm('¿Confirmar esta venta? Se registrará la salida de stock.')) return;
    try {
      await confirmMutation.mutateAsync(id);
    } catch (err: any) {
      setFormError(extractError(err));
    }
  };

  const handleCancel = async () => {
    if (!id) return;
    if (!window.confirm('¿Cancelar esta venta?')) return;
    const reason = window.prompt('Motivo de la cancelación (opcional):');
    if (reason === null) return; // el usuario canceló el prompt
    try {
      await cancelMutation.mutateAsync({ id, reason: reason.trim() || null });
    } catch (err: any) {
      setFormError(extractError(err));
    }
  };

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

  if (isLoading) return <LoadingSpinner />;
  if (isError) return <ErrorMessage message={extractError(error)} />;
  if (!sale) return <ErrorMessage message="Venta no encontrada." />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">Venta {sale.saleNumber}</h1>
        <div className="flex items-center gap-2">
          {sale.paymentType === PaymentType.Credit && (
            <span
              className={`px-3 py-1 rounded-full text-sm font-medium ${PAYMENT_STATUS_BADGE[sale.paymentStatus]}`}
            >
              {sale.paymentStatus === PaymentStatus.Paid
                ? 'Pagado'
                : sale.paymentStatus === PaymentStatus.Partial
                  ? 'Pago Parcial'
                  : 'Pendiente de Pago'}
            </span>
          )}
          <span className={`px-3 py-1 rounded-full text-sm font-medium ${STATUS_BADGE[sale.status]}`}>
            {SALE_STATUS_LABELS[sale.status]}
          </span>
        </div>
      </div>

      {formError && <ErrorMessage message={formError} />}

      {stockErrors.length > 0 && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <h3 className="text-sm font-semibold text-red-700 mb-2">
            Stock insuficiente para los siguientes productos:
          </h3>
          <ul className="list-disc pl-5 text-sm text-red-700 space-y-1">
            {stockErrors.map((e) => (
              <li key={e.productId}>
                <strong>{e.productName ?? e.productId}</strong>: solicitado {e.requestedQuantity},
                disponible {e.availableStock}
              </li>
            ))}
          </ul>
        </div>
      )}

      <div className="bg-white rounded-lg shadow p-5 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 text-sm">
        <Field label="Cliente" value={customerName(sale.customerId)} />
        <Field label="Comprobante" value={VOUCHER_LABELS_MAP[sale.voucherType]} />
        <Field label="Número" value={sale.voucherNumber ?? '—'} />
        <Field label="Fecha" value={new Date(sale.saleDate).toLocaleDateString('es-PE')} />
        <Field label="Moneda" value={sale.currency === Currency.PEN ? 'PEN (S/)' : 'USD ($)'} />
        <Field label="Tipo de cambio" value={String(sale.exchangeRate)} />
        <Field label="Forma de pago" value={paymentLabel} />
        {sale.dueDate && <Field label="Vencimiento" value={new Date(sale.dueDate).toLocaleDateString('es-PE')} />}
        <Field label="Observaciones" value={sale.observations ?? '—'} />
        {sale.status === SaleStatus.Cancelled && (
          <>
            <Field label="Motivo de cancelación" value={sale.cancellationReason ?? '—'} />
            <Field label="Cancelado por" value={sale.cancelledBy ?? '—'} />
            <Field
              label="Fecha de cancelación"
              value={sale.cancelledAt ? new Date(sale.cancelledAt).toLocaleString('es-PE') : '—'}
            />
          </>
        )}
      </div>

      <div className="bg-white rounded-lg shadow p-5">
        <h2 className="text-lg font-semibold text-gray-800 mb-3">Items</h2>
        <Table<Sale['items'][number]>
          rowKey={(it) => it.id}
          columns={[
            { header: 'Producto', accessor: (it) => productName(it.productId) },
            { header: 'Cantidad', accessor: (it) => it.quantity },
            { header: 'Precio Unit.', accessor: (it) => `S/ ${it.unitPrice.toFixed(2)}` },
            { header: 'Desc. %', accessor: (it) => `${it.discountPercentage}%` },
            { header: 'Subtotal', accessor: (it) => `S/ ${it.lineSubtotal.toFixed(2)}` },
          ]}
          data={sale.items}
        />
      </div>

      <div className="bg-white rounded-lg shadow p-5 flex flex-col items-end gap-1 text-sm">
        <div className="flex justify-between w-64">
          <span className="text-gray-600">Subtotal:</span>
          <span className="font-medium">S/ {sale.subtotal.toFixed(2)}</span>
        </div>
        <div className="flex justify-between w-64">
          <span className="text-gray-600">IGV (18%):</span>
          <span className="font-medium">S/ {sale.tax.toFixed(2)}</span>
        </div>
        <div className="flex justify-between w-64 border-t pt-1">
          <span className="text-gray-800 font-semibold">Total:</span>
          <span className="font-bold text-lg">S/ {sale.total.toFixed(2)}</span>
        </div>
      </div>

      <div className="flex gap-3">
        {sale.status === SaleStatus.Draft && (
          <>
            <RequirePermission codes={PermissionCodes.SalesEdit}>
              <Button onClick={() => navigate(`/ventas/${sale.id}/editar`)}>Editar</Button>
            </RequirePermission>
            <RequirePermission codes={PermissionCodes.SalesConfirm}>
              <Button onClick={handleConfirm} disabled={confirmMutation.isPending}>
                Confirmar
              </Button>
            </RequirePermission>
            <RequirePermission codes={PermissionCodes.SalesCancel}>
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
                <span>Debe abrir una caja antes de cancelar ventas al contado.</span>
                <Button variant="secondary" onClick={() => navigate('/caja')}>
                  Ir a Caja
                </Button>
              </div>
            )}
          </>
        )}
        {sale.status === SaleStatus.Confirmed && (
          <>
            {canRegisterPayment && (
              <RequirePermission codes={PermissionCodes.SalesEdit}>
                <Button
                  onClick={handleOpenPaymentModal}
                  disabled={registerPaymentMutation.isPending || cashBlockedForPayment}
                >
                  Registrar Cobro
                </Button>
              </RequirePermission>
            )}
            {canRegisterPayment && cashBlockedForPayment && (
              <div className="flex items-center gap-2 text-sm text-red-600">
                <span>Debe abrir una caja antes de registrar un cobro.</span>
                <Button variant="secondary" onClick={() => navigate('/caja')}>
                  Ir a Caja
                </Button>
              </div>
            )}
            <RequirePermission codes={PermissionCodes.SalesCancel}>
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
                <span>Debe abrir una caja antes de cancelar ventas al contado.</span>
                <Button variant="secondary" onClick={() => navigate('/caja')}>
                  Ir a Caja
                </Button>
              </div>
            )}
          </>
        )}
        <Button variant="secondary" onClick={() => navigate('/ventas')}>
          Volver
        </Button>
      </div>

      <Modal open={paymentModalOpen} title="Registrar Cobro" onClose={() => setPaymentModalOpen(false)}>
        <div className="space-y-4">
          <p className="text-sm text-gray-600">
            Seleccione el método de pago para el cobro total de{' '}
            <strong>S/ {sale.total.toFixed(2)}</strong>.
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
              Confirmar Cobro
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
