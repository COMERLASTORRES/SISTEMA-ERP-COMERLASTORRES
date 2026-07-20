import { useMemo, useState } from 'react';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Table } from '../components/ui/Table';
import { Modal } from '../components/ui/Modal';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import { RequirePermission } from '../components/RequirePermission';
import { PermissionCodes } from '../api/permissionCodes';
import {
  useCustomers,
  useCreateCustomer,
  useUpdateCustomer,
  useDeleteCustomer,
  type Customer,
  type CustomerPayload,
} from '../hooks/useCustomers';
import {
  DocumentType,
  CustomerType,
  DOCUMENT_TYPE_LABELS,
  CUSTOMER_TYPE_LABELS,
  DOCUMENT_HINTS,
} from '../api/customers';

const PAGE_SIZE = 10;

export function CustomersPage() {
  return (
    <RequirePermission
      codes={PermissionCodes.CustomersView}
      fallback={
        <div className="p-6 text-center text-gray-600">
          No tienes permiso para ver este módulo.
        </div>
      }
    >
      <CustomersContent />
    </RequirePermission>
  );
}

function CustomersContent() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Customer | null>(null);
  const [form, setForm] = useState<{
    documentType: DocumentType;
    documentNumber: string;
    name: string;
    email: string;
    phone: string;
    address: string;
    customerType: CustomerType;
    creditLimit: string;
  }>({
    documentType: DocumentType.DNI,
    documentNumber: '',
    name: '',
    email: '',
    phone: '',
    address: '',
    customerType: CustomerType.Regular,
    creditLimit: '0',
  });
  const [formError, setFormError] = useState('');

  const { data, isLoading, isError, error } = useCustomers(page, PAGE_SIZE);
  const createMutation = useCreateCustomer();
  const updateMutation = useUpdateCustomer();
  const deleteMutation = useDeleteCustomer();

  const customers = data?.items ?? [];

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return customers;
    return customers.filter(
      (c) =>
        c.name.toLowerCase().includes(term) ||
        c.documentNumber.toLowerCase().includes(term),
    );
  }, [customers, search]);

  const openCreate = () => {
    setEditing(null);
    setForm({
      documentType: DocumentType.DNI,
      documentNumber: '',
      name: '',
      email: '',
      phone: '',
      address: '',
      customerType: CustomerType.Regular,
      creditLimit: '0',
    });
    setFormError('');
    setModalOpen(true);
  };

  const openEdit = (customer: Customer) => {
    setEditing(customer);
    setForm({
      documentType: customer.documentType,
      documentNumber: customer.documentNumber,
      name: customer.name,
      email: customer.email ?? '',
      phone: customer.phone ?? '',
      address: customer.address ?? '',
      customerType: customer.customerType,
      creditLimit: String(customer.creditLimit),
    });
    setFormError('');
    setModalOpen(true);
  };

  const handleSubmit = async () => {
    setFormError('');
    if (!form.documentNumber.trim()) {
      setFormError('El número de documento es requerido.');
      return;
    }
    if (!form.name.trim()) {
      setFormError('El nombre es requerido.');
      return;
    }
    const payload: CustomerPayload = {
      documentType: form.documentType,
      documentNumber: form.documentNumber.trim(),
      name: form.name.trim(),
      email: form.email.trim() || null,
      phone: form.phone.trim() || null,
      address: form.address.trim() || null,
      customerType: form.customerType,
      creditLimit: Number(form.creditLimit) || 0,
    };
    try {
      if (editing) {
        await updateMutation.mutateAsync({ id: editing.id, payload });
      } else {
        await createMutation.mutateAsync(payload);
      }
      setModalOpen(false);
    } catch (err: any) {
      setFormError(translateError(extractError(err)));
    }
  };

  const handleDelete = async (customer: Customer) => {
    if (!window.confirm(`¿Eliminar el cliente "${customer.name}"?`)) return;
    try {
      await deleteMutation.mutateAsync(customer.id);
    } catch (err: any) {
      window.alert(translateError(extractError(err)));
    }
  };

  if (isLoading) return <LoadingSpinner />;
  if (isError) return <ErrorMessage message={extractError(error)} />;

  const totalPages = data ? Math.max(1, Math.ceil(data.total / PAGE_SIZE)) : 1;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">Clientes</h1>
        <RequirePermission
          codes={PermissionCodes.CustomersCreate}
          fallback={
            <span className="text-sm text-gray-500">
              No tienes permiso para crear clientes.
            </span>
          }
        >
          <Button onClick={openCreate}>Nuevo Cliente</Button>
        </RequirePermission>
      </div>

      <Input
        placeholder="Buscar por nombre o número de documento..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />

      <Table<Customer>
        rowKey={(c) => c.id}
        columns={[
          {
            header: 'Documento',
            accessor: (c) => `${DOCUMENT_TYPE_LABELS[c.documentType]} ${c.documentNumber}`,
          },
          { header: 'Nombre', accessor: (c) => c.name },
          { header: 'Tipo Cliente', accessor: (c) => CUSTOMER_TYPE_LABELS[c.customerType] },
          { header: 'Email', accessor: (c) => c.email ?? '—' },
          { header: 'Teléfono', accessor: (c) => c.phone ?? '—' },
          {
            header: 'Estado',
            accessor: (c) =>
              c.isActive ? (
                <span className="text-green-700 font-medium">Activo</span>
              ) : (
                <span className="text-red-600 font-medium">Inactivo</span>
              ),
          },
          {
            header: 'Acciones',
            accessor: (c) => (
              <div className="flex gap-2">
                <RequirePermission codes={PermissionCodes.CustomersEdit}>
                  <Button variant="secondary" onClick={() => openEdit(c)}>
                    Editar
                  </Button>
                </RequirePermission>
                <RequirePermission codes={PermissionCodes.CustomersDelete}>
                  <Button variant="danger" onClick={() => handleDelete(c)}>
                    Eliminar
                  </Button>
                </RequirePermission>
              </div>
            ),
          },
        ]}
        data={filtered}
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

      <Modal
        open={modalOpen}
        title={editing ? 'Editar Cliente' : 'Nuevo Cliente'}
        onClose={() => setModalOpen(false)}
      >
        <div className="space-y-4">
          {formError && <ErrorMessage message={formError} />}

          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Tipo de Documento</label>
            <select
              className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={form.documentType}
              onChange={(e) =>
                setForm((f) => ({ ...f, documentType: Number(e.target.value) as DocumentType }))
              }
            >
              {Object.values(DocumentType)
                .filter((v) => typeof v === 'number')
                .map((v) => (
                  <option key={v} value={v}>
                    {DOCUMENT_TYPE_LABELS[v as DocumentType]}
                  </option>
                ))}
            </select>
          </div>

          <Input
            label="Número de Documento"
            value={form.documentNumber}
            onChange={(e) => setForm((f) => ({ ...f, documentNumber: e.target.value }))}
            placeholder={DOCUMENT_HINTS[form.documentType]}
            hint={DOCUMENT_HINTS[form.documentType]}
          />

          <Input
            label="Nombre"
            value={form.name}
            onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
          />

          <Input
            label="Email (opcional)"
            type="email"
            value={form.email}
            onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
          />

          <Input
            label="Teléfono (opcional)"
            value={form.phone}
            onChange={(e) => setForm((f) => ({ ...f, phone: e.target.value }))}
          />

          <Input
            label="Dirección (opcional)"
            value={form.address}
            onChange={(e) => setForm((f) => ({ ...f, address: e.target.value }))}
          />

          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Tipo de Cliente</label>
            <select
              className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={form.customerType}
              onChange={(e) =>
                setForm((f) => ({ ...f, customerType: Number(e.target.value) as CustomerType }))
              }
            >
              {Object.values(CustomerType)
                .filter((v) => typeof v === 'number')
                .map((v) => (
                  <option key={v} value={v}>
                    {CUSTOMER_TYPE_LABELS[v as CustomerType]}
                  </option>
                ))}
            </select>
          </div>

          <Input
            label="Límite de Crédito"
            type="number"
            value={form.creditLimit}
            onChange={(e) => setForm((f) => ({ ...f, creditLimit: e.target.value }))}
          />

          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={() => setModalOpen(false)}>
              Cancelar
            </Button>
            <Button
              onClick={handleSubmit}
              disabled={createMutation.isPending || updateMutation.isPending}
            >
              {editing ? 'Guardar' : 'Crear'}
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

function translateError(message: string): string {
  if (message.includes('Corporate customers require DocumentType RUC')) {
    return 'Los clientes corporativos requieren RUC.';
  }
  if (message.includes('already exists')) {
    return 'Ya existe un cliente con este documento.';
  }
  return message;
}
