import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Table } from '../components/ui/Table';
import { Modal } from '../components/ui/Modal';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import {
  useOpenCashRegister,
  useOpenCashRegisterMutation,
  useCloseCashRegisterMutation,
  useRegisterCashMovementMutation,
} from '../hooks/useCashRegisters';
import {
  CashMovementType,
  MovementReason,
  CASH_REGISTER_STATUS_LABELS,
  CASH_MOVEMENT_TYPE_LABELS,
  MOVEMENT_REASON_LABELS,
  PAYMENT_METHOD_LABELS,
  type CashRegister,
} from '../api/cashRegisters';
import { PaymentMethod } from '../api/sales';

function extractError(err: any): string {
  if (err?.response?.data) {
    if (typeof err.response.data === 'string') return err.response.data;
    if (typeof err.response.data.message === 'string') return err.response.data.message;
  }
  return 'Ocurrió un error inesperado.';
}

export function CashRegisterPage() {
  const navigate = useNavigate();
  const { data: register, isLoading, isError, error } = useOpenCashRegister();

  const openMutation = useOpenCashRegisterMutation();
  const closeMutation = useCloseCashRegisterMutation();
  const movementMutation = useRegisterCashMovementMutation();

  const [formError, setFormError] = useState('');

  if (isLoading) return <LoadingSpinner />;
  if (isError) return <ErrorMessage message={extractError(error)} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">Caja</h1>
        <Button variant="secondary" onClick={() => navigate('/caja/historial')}>
          Historial
        </Button>
      </div>

      {formError && <ErrorMessage message={formError} />}

      {register == null ? (
        <OpenCashRegisterForm
          onOpen={async (payload) => {
            setFormError('');
            try {
              await openMutation.mutateAsync(payload);
            } catch (err: any) {
              setFormError(extractError(err));
            }
          }}
          pending={openMutation.isPending}
        />
      ) : (
        <OpenCashRegisterDetail
          register={register}
          onRegisterMovement={async (payload) => {
            setFormError('');
            try {
              await movementMutation.mutateAsync({ id: register.id, payload });
              return true;
            } catch (err: any) {
              setFormError(extractError(err));
              return false;
            }
          }}
          onClose={async (closingAmount, notes) => {
            setFormError('');
            try {
              await closeMutation.mutateAsync({
                id: register.id,
                payload: { closingAmount, notes },
              });
            } catch (err: any) {
              setFormError(extractError(err));
              return false;
            }
            return true;
          }}
          movementPending={movementMutation.isPending}
          closePending={closeMutation.isPending}
        />
      )}
    </div>
  );
}

interface OpenFormProps {
  onOpen: (payload: { openingAmount: number; notes?: string | null }) => Promise<void>;
  pending: boolean;
}

function OpenCashRegisterForm({ onOpen, pending }: OpenFormProps) {
  const [openingAmount, setOpeningAmount] = useState('0');
  const [notes, setNotes] = useState('');

  const handleSubmit = async () => {
    const amount = Number(openingAmount) || 0;
    if (amount < 0) return;
    await onOpen({ openingAmount: amount, notes: notes.trim() || null });
  };

  return (
    <div className="bg-white rounded-lg shadow p-6 max-w-md space-y-4">
      <h2 className="text-lg font-semibold text-gray-800">Abrir Caja</h2>
      <Input
        label="Monto de apertura (S/)"
        type="number"
        value={openingAmount}
        onChange={(e) => setOpeningAmount(e.target.value)}
      />
      <div className="flex flex-col gap-1">
        <label className="text-sm font-medium text-gray-700">Notas (opcional)</label>
        <textarea
          className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows={2}
        />
      </div>
      <Button onClick={handleSubmit} disabled={pending}>
        Abrir Caja
      </Button>
    </div>
  );
}

interface DetailProps {
  register: CashRegister;
  onRegisterMovement: (payload: any) => Promise<boolean>;
  onClose: (closingAmount: number, notes?: string | null) => Promise<boolean>;
  movementPending: boolean;
  closePending: boolean;
}

function OpenCashRegisterDetail({ register, onRegisterMovement, onClose, movementPending, closePending }: DetailProps) {
  const [movementModal, setMovementModal] = useState(false);
  const [closeModal, setCloseModal] = useState(false);

  // Normalización de raíz: protege contra cualquier campo numérico/arreglo undefined
  // que pudiera venir de la API, evitando el error "reading 'toFixed' of undefined".
  const safeRegister = {
    ...register,
    movements: register.movements ?? [],
    openingAmount: Number(register.openingAmount ?? 0),
  };

  const movements = safeRegister.movements;

  const income = useMemo(
    () => movements.filter((m) => m.type === CashMovementType.Income).reduce((a, m) => a + m.amount, 0),
    [movements],
  );
  const expense = useMemo(
    () => movements.filter((m) => m.type === CashMovementType.Expense).reduce((a, m) => a + m.amount, 0),
    [movements],
  );
  const liveTotal = safeRegister.openingAmount + income - expense;

  return (
    <div className="space-y-6">
      <div className="bg-white rounded-lg shadow p-5 grid grid-cols-1 md:grid-cols-3 gap-4">
        <Field label="Número de caja" value={register.cashRegisterNumber} />
        <Field label="Estado" value={CASH_REGISTER_STATUS_LABELS[register.status]} />
        <Field label="Apertura" value={`S/ ${safeRegister.openingAmount.toFixed(2)}`} />
      </div>

      <div className="bg-white rounded-lg shadow p-5">
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-lg font-semibold text-gray-800">Movimientos</h2>
          <div className="flex gap-2">
            <Button variant="secondary" onClick={() => setMovementModal(true)}>
              Registrar Movimiento
            </Button>
            <Button variant="danger" onClick={() => setCloseModal(true)} disabled={closePending}>
              Cerrar Caja
            </Button>
          </div>
        </div>

        <Table<CashRegister['movements'][number]>
          rowKey={(m) => m.id}
          columns={[
            {
              header: 'Fecha',
              accessor: (m) => new Date(m.createdAt).toLocaleString('es-PE'),
            },
            {
              header: 'Tipo',
              accessor: (m) => (
                <span
                  className={`px-2 py-1 rounded-full text-xs font-medium ${
                    m.type === CashMovementType.Income
                      ? 'bg-green-100 text-green-700'
                      : 'bg-red-100 text-red-700'
                  }`}
                >
                  {CASH_MOVEMENT_TYPE_LABELS[m.type]}
                </span>
              ),
            },
            { header: 'Motivo', accessor: (m) => MOVEMENT_REASON_LABELS[m.reason] },
            { header: 'Método', accessor: (m) => PAYMENT_METHOD_LABELS[m.paymentMethod] },
            { header: 'Monto', accessor: (m) => `S/ ${Number(m.amount ?? 0).toFixed(2)}` },
            { header: 'Descripción', accessor: (m) => m.description || '—' },
          ]}
          data={register.movements ?? []}
        />
      </div>

      <div className="bg-white rounded-lg shadow p-5 flex flex-col items-end gap-1 text-sm">
        <div className="flex justify-between w-64">
          <span className="text-gray-600">Apertura:</span>
          <span className="font-medium">S/ {safeRegister.openingAmount.toFixed(2)}</span>
        </div>
        <div className="flex justify-between w-64">
          <span className="text-gray-600">Ingresos:</span>
          <span className="font-medium text-green-700">S/ {income.toFixed(2)}</span>
        </div>
        <div className="flex justify-between w-64">
          <span className="text-gray-600">Egresos:</span>
          <span className="font-medium text-red-700">S/ {expense.toFixed(2)}</span>
        </div>
        <div className="flex justify-between w-64 border-t pt-1">
          <span className="text-gray-800 font-semibold">Total en caja:</span>
          <span className="font-bold text-lg">S/ {liveTotal.toFixed(2)}</span>
        </div>
      </div>

      <RegisterMovementModal
        open={movementModal}
        onClose={() => setMovementModal(false)}
        onSave={async (payload) => {
          const ok = await onRegisterMovement(payload);
          if (ok) setMovementModal(false);
        }}
        pending={movementPending}
      />

      <CloseCashRegisterModal
        open={closeModal}
        expected={liveTotal}
        onCancel={() => setCloseModal(false)}
        onConfirm={async (closingAmount, notes) => {
          const ok = await onClose(closingAmount, notes);
          if (ok) setCloseModal(false);
          return ok;
        }}
        pending={closePending}
      />
    </div>
  );
}

interface RegisterMovementModalProps {
  open: boolean;
  onClose: () => void;
  onSave: (payload: any) => Promise<void>;
  pending: boolean;
}

function RegisterMovementModal({ open, onClose, onSave, pending }: RegisterMovementModalProps) {
  const [type, setType] = useState<CashMovementType>(CashMovementType.Expense);
  const [reason, setReason] = useState<MovementReason>(MovementReason.CashWithdrawal);
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>(PaymentMethod.Cash);
  const [amount, setAmount] = useState('0');
  const [description, setDescription] = useState('');

  const handleSave = async () => {
    const amt = Number(amount) || 0;
    if (amt <= 0) return;
    await onSave({
      type,
      reason,
      paymentMethod,
      amount: amt,
      description: description.trim() || '—',
    });
  };

  return (
    <Modal open={open} title="Registrar Movimiento" onClose={onClose}>
      <div className="space-y-4">
        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-gray-700">Tipo</label>
          <select
            className="px-3 py-2 border border-gray-300 rounded-md text-sm"
            value={type}
            onChange={(e) => setType(Number(e.target.value) as CashMovementType)}
          >
            {Object.values(CashMovementType)
              .filter((v) => typeof v === 'number')
              .map((v) => (
                <option key={v} value={v}>
                  {CASH_MOVEMENT_TYPE_LABELS[v as CashMovementType]}
                </option>
              ))}
          </select>
        </div>

        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-gray-700">Motivo</label>
          <select
            className="px-3 py-2 border border-gray-300 rounded-md text-sm"
            value={reason}
            onChange={(e) => setReason(Number(e.target.value) as MovementReason)}
          >
            {Object.values(MovementReason)
              .filter((v) => typeof v === 'number')
              .map((v) => (
                <option key={v} value={v}>
                  {MOVEMENT_REASON_LABELS[v as MovementReason]}
                </option>
              ))}
          </select>
        </div>

        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-gray-700">Método de pago</label>
          <select
            className="px-3 py-2 border border-gray-300 rounded-md text-sm"
            value={paymentMethod}
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

        <Input
          label="Monto (S/)"
          type="number"
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
        />

        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-gray-700">Descripción</label>
          <textarea
            className="px-3 py-2 border border-gray-300 rounded-md text-sm"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={2}
          />
        </div>

        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button onClick={handleSave} disabled={pending}>
            Guardar
          </Button>
        </div>
      </div>
    </Modal>
  );
}

interface CloseCashRegisterModalProps {
  open: boolean;
  expected: number;
  onCancel: () => void;
  onConfirm: (closingAmount: number, notes?: string | null) => Promise<boolean>;
  pending: boolean;
}

function CloseCashRegisterModal({ open, expected, onCancel, onConfirm, pending }: CloseCashRegisterModalProps) {
  const [closingAmount, setClosingAmount] = useState('');
  const [notes, setNotes] = useState('');
  const [result, setResult] = useState<{ counted: number; difference: number } | null>(null);

  const handleConfirm = async () => {
    const counted = Number(closingAmount) || 0;
    const difference = counted - expected;
    setResult({ counted, difference });
    // El cierre real se confirma con el botón "Confirmar cierre".
    return false;
  };

  const handleFinalClose = async () => {
    const counted = Number(closingAmount) || 0;
    const ok = await onConfirm(counted, notes.trim() || null);
    if (ok) setResult(null);
  };

  return (
    <Modal open={open} title="Cerrar Caja" onClose={onCancel}>
      <div className="space-y-4">
        <Input
          label="Monto contado físicamente (S/)"
          type="number"
          value={closingAmount}
          onChange={(e) => setClosingAmount(e.target.value)}
        />
        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-gray-700">Notas (opcional)</label>
          <textarea
            className="px-3 py-2 border border-gray-300 rounded-md text-sm"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={2}
          />
        </div>

        <div className="bg-gray-50 rounded p-3 text-sm space-y-1">
          <div className="flex justify-between">
            <span className="text-gray-600">Esperado:</span>
            <span className="font-medium">S/ {expected.toFixed(2)}</span>
          </div>
          {result && (
            <>
              <div className="flex justify-between">
                <span className="text-gray-600">Contado:</span>
                <span className="font-medium">S/ {result.counted.toFixed(2)}</span>
              </div>
              <div className="flex justify-between border-t pt-1">
                <span className="text-gray-800 font-semibold">Diferencia:</span>
                <span
                  className={`font-bold ${
                    result.difference === 0
                      ? 'text-gray-800'
                      : result.difference > 0
                        ? 'text-green-700'
                        : 'text-red-700'
                  }`}
                >
                  S/ {result.difference.toFixed(2)}
                </span>
              </div>
            </>
          )}
        </div>

        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={onCancel}>
            Cancelar
          </Button>
          {result == null ? (
            <Button onClick={handleConfirm} disabled={pending}>
              Calcular diferencia
            </Button>
          ) : (
            <Button variant="danger" onClick={handleFinalClose} disabled={pending}>
              Confirmar cierre
            </Button>
          )}
        </div>
      </div>
    </Modal>
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
