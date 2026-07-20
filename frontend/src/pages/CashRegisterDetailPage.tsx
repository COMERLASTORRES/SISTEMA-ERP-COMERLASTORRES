import { useNavigate, useParams } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { Table } from '../components/ui/Table';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import { RequirePermission } from '../components/RequirePermission';
import { PermissionCodes } from '../api/permissionCodes';
import { useCashRegister } from '../hooks/useCashRegisters';
import {
  CashRegisterStatus,
  CashMovementType,
  CASH_REGISTER_STATUS_LABELS,
  CASH_MOVEMENT_TYPE_LABELS,
  MOVEMENT_REASON_LABELS,
  PAYMENT_METHOD_LABELS,
  type CashRegister,
} from '../api/cashRegisters';

const STATUS_BADGE: Record<CashRegisterStatus, string> = {
  [CashRegisterStatus.Open]: 'bg-green-100 text-green-700',
  [CashRegisterStatus.Closed]: 'bg-gray-100 text-gray-700',
};

export function CashRegisterDetailPage() {
  return (
    <RequirePermission
      codes={PermissionCodes.CashRegisterView}
      fallback={
        <div className="p-6 text-center text-gray-600">
          No tienes permiso para ver este módulo.
        </div>
      }
    >
      <CashRegisterDetailContent />
    </RequirePermission>
  );
}

function CashRegisterDetailContent() {
  const { id } = useParams();
  const navigate = useNavigate();

  const { data: register, isLoading, isError, error } = useCashRegister(id);

  if (isLoading) return <LoadingSpinner />;
  if (isError) return <ErrorMessage message={extractError(error)} />;
  if (!register) return <ErrorMessage message="Caja no encontrada." />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">Caja {register.cashRegisterNumber}</h1>
        <span className={`px-3 py-1 rounded-full text-sm font-medium ${STATUS_BADGE[register.status]}`}>
          {CASH_REGISTER_STATUS_LABELS[register.status]}
        </span>
      </div>

      <div className="bg-white rounded-lg shadow p-5 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 text-sm">
        <Field label="Usuario" value={register.userId} />
        <Field label="Fecha de apertura" value={new Date(register.openingDate).toLocaleString('es-PE')} />
        <Field
          label="Fecha de cierre"
          value={register.closingDate ? new Date(register.closingDate).toLocaleString('es-PE') : '—'}
        />
        <Field label="Monto de apertura" value={`S/ ${register.openingAmount.toFixed(2)}`} />
        <Field
          label="Monto de cierre"
          value={register.closingAmount != null ? `S/ ${register.closingAmount.toFixed(2)}` : '—'}
        />
        <Field
          label="Esperado"
          value={register.expectedAmount != null ? `S/ ${register.expectedAmount.toFixed(2)}` : '—'}
        />
        <Field
          label="Diferencia"
          value={register.difference != null ? `S/ ${register.difference.toFixed(2)}` : '—'}
        />
        <Field label="Notas" value={register.notes ?? '—'} />
      </div>

      <div className="bg-white rounded-lg shadow p-5">
        <h2 className="text-lg font-semibold text-gray-800 mb-3">Movimientos</h2>
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
            { header: 'Monto', accessor: (m) => `S/ ${m.amount.toFixed(2)}` },
            { header: 'Descripción', accessor: (m) => m.description || '—' },
          ]}
          data={register.movements ?? []}
        />
      </div>

      <div className="flex gap-3">
        <Button variant="secondary" onClick={() => navigate('/caja/historial')}>
          Volver al historial
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
