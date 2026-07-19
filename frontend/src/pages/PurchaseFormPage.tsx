import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Table } from '../components/ui/Table';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import { useProducts } from '../hooks/useProducts';
import { useSuppliers } from '../hooks/useSuppliers';
import { useOpenCashRegister } from '../hooks/useCashRegisters';
import {
  usePurchase,
  useCreatePurchase,
  useUpdatePurchase,
  useConfirmPurchase,
  useCancelPurchase,
} from '../hooks/usePurchases';
import {
  PurchaseStatus,
  VoucherType,
  Currency,
  PaymentType,
  PaymentMethod,
  PURCHASE_STATUS_LABELS,
  PAYMENT_TYPE_LABELS,
  PAYMENT_METHOD_LABELS,
  type UpdatePurchasePayload,
} from '../api/purchases';

const TAX_RATE = 0.18;

interface ItemRow {
  productId: string;
  quantity: string;
  unitCost: string;
  discountPercentage: string;
}

function emptyItem(): ItemRow {
  return { productId: '', quantity: '1', unitCost: '0', discountPercentage: '0' };
}

export function PurchaseFormPage() {
  const { id } = useParams();
  const isEdit = !!id;
  const navigate = useNavigate();

  const { data: productsData } = useProducts(1, 1000);
  const products = productsData?.items ?? [];
  const { data: suppliersData } = useSuppliers(1, 1000);
  const suppliers = suppliersData?.items ?? [];

  const { data: purchase, isLoading, isError, error } = usePurchase(id);
  const createMutation = useCreatePurchase();
  const updateMutation = useUpdatePurchase();
  const confirmMutation = useConfirmPurchase();
  const cancelMutation = useCancelPurchase();
  const { data: openCashRegister } = useOpenCashRegister();

  const [supplierId, setSupplierId] = useState('');
  const [voucherType, setVoucherType] = useState<VoucherType>(VoucherType.Factura);
  const [voucherNumber, setVoucherNumber] = useState('');
  const [purchaseDate, setPurchaseDate] = useState(new Date().toISOString().slice(0, 10));
  const [currency, setCurrency] = useState<Currency>(Currency.PEN);
  const [exchangeRate, setExchangeRate] = useState('1');
  const [paymentType, setPaymentType] = useState<PaymentType>(PaymentType.Cash);
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod | ''>(PaymentMethod.Cash);
  const [creditDays, setCreditDays] = useState('0');
  const [observations, setObservations] = useState('');
  const [items, setItems] = useState<ItemRow[]>([emptyItem()]);
  const [formError, setFormError] = useState('');

  // Cargar datos al editar
  useEffect(() => {
    if (!purchase) return;
    setSupplierId(purchase.supplierId);
    setVoucherType(purchase.voucherType);
    setVoucherNumber(purchase.voucherNumber);
    setPurchaseDate(purchase.purchaseDate.slice(0, 10));
    setCurrency(purchase.currency);
    setExchangeRate(String(purchase.exchangeRate));
    setPaymentType(purchase.paymentType);
    setPaymentMethod(purchase.paymentMethod ?? PaymentMethod.Cash);
    setCreditDays(String(purchase.creditDays ?? 0));
    setObservations(purchase.observations ?? '');
    setItems(
      purchase.items.map((i) => ({
        productId: i.productId,
        quantity: String(i.quantity),
        unitCost: String(i.unitCost),
        discountPercentage: String(i.discountPercentage),
      })),
    );
  }, [purchase]);

  const lineSubtotal = (it: ItemRow): number => {
    const q = Number(it.quantity) || 0;
    const c = Number(it.unitCost) || 0;
    const d = Number(it.discountPercentage) || 0;
    return q * c * (1 - d / 100);
  };

  const subtotal = useMemo(() => items.reduce((acc, it) => acc + lineSubtotal(it), 0), [items]);
  const tax = subtotal * TAX_RATE;
  const total = subtotal + tax;

  const updateItem = (idx: number, patch: Partial<ItemRow>) => {
    setItems((prev) => prev.map((it, i) => (i === idx ? { ...it, ...patch } : it)));
  };

  const addItem = () => setItems((prev) => [...prev, emptyItem()]);

  const removeItem = (idx: number) =>
    setItems((prev) => (prev.length > 1 ? prev.filter((_, i) => i !== idx) : prev));

  const validate = (): string | null => {
    if (!supplierId) return 'Debe seleccionar un proveedor.';
    if (!voucherNumber.trim()) return 'El número de comprobante es requerido.';
    if (items.length === 0) return 'Debe haber al menos un item.';
    for (const it of items) {
      if (!it.productId) return 'Todos los items deben tener un producto válido.';
      if ((Number(it.quantity) || 0) <= 0) return 'La cantidad debe ser mayor a 0.';
      if ((Number(it.unitCost) || 0) <= 0) return 'El costo unitario debe ser mayor a 0.';
    }
    if (paymentType === PaymentType.Cash && paymentMethod === '') {
      return 'Debe indicar el método de pago para compras al contado.';
    }
    if (paymentType === PaymentType.Credit) {
      if ((Number(creditDays) || 0) <= 0) return 'Debe indicar los días de crédito para compras a crédito.';
    }
    return null;
  };

  const buildPayload = (): UpdatePurchasePayload => ({
    id: id ?? '',
    supplierId,
    voucherType,
    voucherNumber: voucherNumber.trim(),
    purchaseDate: new Date(purchaseDate).toISOString(),
    currency,
    exchangeRate: Number(exchangeRate) || 1,
    paymentType,
    paymentMethod:
      paymentType === PaymentType.Cash && paymentMethod !== '' ? (paymentMethod as PaymentMethod) : null,
    creditDays: paymentType === PaymentType.Credit ? Number(creditDays) : null,
    observations: observations.trim() || null,
    items: items.map((it) => ({
      productId: it.productId,
      quantity: Number(it.quantity),
      unitCost: Number(it.unitCost),
      discountPercentage: Number(it.discountPercentage) || 0,
    })),
  });

  const handleSave = async () => {
    setFormError('');
    const err = validate();
    if (err) {
      setFormError(err);
      return;
    }
    try {
      if (isEdit && id) {
        await updateMutation.mutateAsync({ id, payload: buildPayload() });
      } else {
        await createMutation.mutateAsync(buildPayload());
      }
      navigate('/compras');
    } catch (err: any) {
      setFormError(extractError(err));
    }
  };

  const handleConfirm = async () => {
    if (!id) return;
    // Para compras al contado, se requiere una caja abierta del usuario.
    if (paymentType === PaymentType.Cash && openCashRegister == null) {
      setFormError('Debe abrir una caja antes de confirmar compras al contado.');
      return;
    }
    if (!window.confirm('¿Confirmar esta compra? Se registrará el ingreso de stock.')) return;
    try {
      await confirmMutation.mutateAsync(id);
      navigate('/compras');
    } catch (err: any) {
      setFormError(extractError(err));
    }
  };

  const handleCancel = async () => {
    if (!id) return;
    if (!window.confirm('¿Cancelar esta compra?')) return;
    const reason = window.prompt('Motivo de la cancelación (opcional):');
    if (reason === null) return; // el usuario canceló el prompt
    try {
      await cancelMutation.mutateAsync({ id, reason: reason.trim() || null });
      navigate('/compras');
    } catch (err: any) {
      setFormError(extractError(err));
    }
  };

  if (isEdit && isLoading) return <LoadingSpinner />;
  if (isEdit && isError) return <ErrorMessage message={extractError(error)} />;

  const readOnly = isEdit && purchase?.status !== PurchaseStatus.Draft;
  const isDraft = !isEdit || purchase?.status === PurchaseStatus.Draft;
  const isCancelled = purchase?.status === PurchaseStatus.Cancelled;

  // Para compras al contado, se requiere una caja abierta del usuario. Si no la hay,
  // se bloquea el botón Confirmar y se muestra un aviso con link a /caja.
  const cashBlockedForConfirm =
    isDraft && paymentType === PaymentType.Cash && openCashRegister == null;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">
          {isEdit ? `Compra ${purchase?.purchaseNumber}` : 'Nueva Compra'}
        </h1>
        {isEdit && purchase && (
          <span className="text-sm font-medium text-gray-600">
            Estado: {PURCHASE_STATUS_LABELS[purchase.status]}
          </span>
        )}
      </div>

      {formError && <ErrorMessage message={formError} />}

      {/* Cabecera */}
      <div className="bg-white rounded-lg shadow p-5 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-gray-700">Proveedor</label>
          <select
            className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={supplierId}
            disabled={readOnly}
            onChange={(e) => setSupplierId(e.target.value)}
          >
            <option value="">Seleccione...</option>
            {suppliers.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
        </div>

        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-gray-700">Tipo de comprobante</label>
          <select
            className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={voucherType}
            disabled={readOnly}
            onChange={(e) => setVoucherType(Number(e.target.value) as VoucherType)}
          >
            {Object.values(VoucherType)
              .filter((v) => typeof v === 'number')
              .map((v) => (
                <option key={v} value={v}>
                  {v === VoucherType.Boleta
                    ? 'Boleta'
                    : v === VoucherType.Factura
                      ? 'Factura'
                      : v === VoucherType.NotaCredito
                        ? 'Nota de Crédito'
                        : 'Otro'}
                </option>
              ))}
          </select>
        </div>

        <Input
          label="Número de comprobante"
          value={voucherNumber}
          disabled={readOnly}
          onChange={(e) => setVoucherNumber(e.target.value)}
        />

        <Input
          label="Fecha"
          type="date"
          value={purchaseDate}
          disabled={readOnly}
          onChange={(e) => setPurchaseDate(e.target.value)}
        />

        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-gray-700">Moneda</label>
          <select
            className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={currency}
            disabled={readOnly}
            onChange={(e) => setCurrency(Number(e.target.value) as Currency)}
          >
            <option value={Currency.PEN}>PEN (S/)</option>
            <option value={Currency.USD}>USD ($)</option>
          </select>
        </div>

        <Input
          label="Tipo de cambio"
          type="number"
          value={exchangeRate}
          disabled={readOnly || currency === Currency.PEN}
          onChange={(e) => setExchangeRate(e.target.value)}
        />

        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-gray-700">Tipo de pago</label>
          <select
            className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={paymentType}
            disabled={readOnly}
            onChange={(e) => setPaymentType(Number(e.target.value) as PaymentType)}
          >
            {Object.values(PaymentType)
              .filter((v) => typeof v === 'number')
              .map((v) => (
                <option key={v} value={v}>
                  {PAYMENT_TYPE_LABELS[v as PaymentType]}
                </option>
              ))}
          </select>
        </div>

        {paymentType === PaymentType.Cash && (
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Método de pago</label>
            <select
              className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={paymentMethod}
              disabled={readOnly}
              onChange={(e) => setPaymentMethod(Number(e.target.value) as PaymentMethod)}
            >
              {Object.values(PaymentMethod)
                .filter((v) => typeof v === 'number')
                .map((v) => (
                  <option key={v} value={v}>
                    {PAYMENT_METHOD_LABELS[v as PaymentMethod]}
                  </option>
                ))}
            </select>
          </div>
        )}

        {paymentType === PaymentType.Credit && (
          <Input
            label="Días de crédito"
            type="number"
            value={creditDays}
            disabled={readOnly}
            onChange={(e) => setCreditDays(e.target.value)}
          />
        )}

        <div className="flex flex-col gap-1 md:col-span-3">
          <label className="text-sm font-medium text-gray-700">Observaciones</label>
          <textarea
            className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={observations}
            disabled={readOnly}
            onChange={(e) => setObservations(e.target.value)}
            rows={2}
          />
        </div>
      </div>

      {/* Tabla dinámica de items */}
      <div className="bg-white rounded-lg shadow p-5">
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-lg font-semibold text-gray-800">Items</h2>
          {!readOnly && (
            <Button variant="secondary" onClick={addItem}>
              Agregar item
            </Button>
          )}
        </div>

        <Table<ItemRow & { idx: number }>
          rowKey={(row) => String(row.idx)}
          columns={[
            {
              header: 'Producto',
              accessor: (row) => (
                <select
                  className="px-2 py-1 border border-gray-300 rounded-md text-sm w-full"
                  value={row.productId}
                  disabled={readOnly}
                  onChange={(e) => updateItem(row.idx, { productId: e.target.value })}
                >
                  <option value="">Seleccione...</option>
                  {products.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.name}
                    </option>
                  ))}
                </select>
              ),
            },
            {
              header: 'Cantidad',
              accessor: (row) => (
                <input
                  type="number"
                  className="px-2 py-1 border border-gray-300 rounded-md text-sm w-24"
                  value={row.quantity}
                  disabled={readOnly}
                  onChange={(e) => updateItem(row.idx, { quantity: e.target.value })}
                />
              ),
            },
            {
              header: 'Costo Unit.',
              accessor: (row) => (
                <input
                  type="number"
                  className="px-2 py-1 border border-gray-300 rounded-md text-sm w-28"
                  value={row.unitCost}
                  disabled={readOnly}
                  onChange={(e) => updateItem(row.idx, { unitCost: e.target.value })}
                />
              ),
            },
            {
              header: 'Desc. %',
              accessor: (row) => (
                <input
                  type="number"
                  className="px-2 py-1 border border-gray-300 rounded-md text-sm w-20"
                  value={row.discountPercentage}
                  disabled={readOnly}
                  onChange={(e) => updateItem(row.idx, { discountPercentage: e.target.value })}
                />
              ),
            },
            {
              header: 'Subtotal',
              accessor: (row) => `S/ ${lineSubtotal(row).toFixed(2)}`,
            },
            {
              header: '',
              accessor: (row) =>
                !readOnly && items.length > 1 ? (
                  <Button variant="danger" onClick={() => removeItem(row.idx)}>
                    Quitar
                  </Button>
                ) : null,
            },
          ]}
          data={items.map((it, idx) => ({ ...it, idx }))}
        />
      </div>

      {/* Totales */}
      <div className="bg-white rounded-lg shadow p-5 flex flex-col items-end gap-1 text-sm">
        <div className="flex justify-between w-64">
          <span className="text-gray-600">Subtotal:</span>
          <span className="font-medium">S/ {subtotal.toFixed(2)}</span>
        </div>
        <div className="flex justify-between w-64">
          <span className="text-gray-600">IGV (18%):</span>
          <span className="font-medium">S/ {tax.toFixed(2)}</span>
        </div>
        <div className="flex justify-between w-64 border-t pt-1">
          <span className="text-gray-800 font-semibold">Total:</span>
          <span className="font-bold text-lg">S/ {total.toFixed(2)}</span>
        </div>
      </div>

      {/* Acciones */}
      <div className="flex gap-3">
        {isDraft && (
          <Button onClick={handleSave} disabled={createMutation.isPending || updateMutation.isPending}>
            Guardar Borrador
          </Button>
        )}
        {isDraft && id && (
          <>
            <Button
              onClick={handleConfirm}
              disabled={confirmMutation.isPending || cashBlockedForConfirm}
            >
              Confirmar
            </Button>
            {cashBlockedForConfirm && (
              <div className="flex items-center gap-2 text-sm text-red-600">
                <span>Debe abrir una caja antes de confirmar compras al contado.</span>
                <Button variant="secondary" onClick={() => navigate('/caja')}>
                  Ir a Caja
                </Button>
              </div>
            )}
          </>
        )}
        {!isCancelled && id && (
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

function extractError(err: any): string {
  if (err?.response?.data) {
    if (typeof err.response.data === 'string') return err.response.data;
    if (typeof err.response.data.message === 'string') return err.response.data.message;
  }
  return 'Ocurrió un error inesperado.';
}
