import { useMemo, useState } from 'react';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Table } from '../components/ui/Table';
import { Modal } from '../components/ui/Modal';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import {
  useSuppliers,
  useCreateSupplier,
  useUpdateSupplier,
  useDeleteSupplier,
  type Supplier,
  type SupplierPayload,
} from '../hooks/useSuppliers';
import {
  DocumentType,
  DOCUMENT_TYPE_LABELS,
  DOCUMENT_HINTS,
} from '../api/suppliers';

const PAGE_SIZE = 10;

export function SuppliersPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Supplier | null>(null);
  const [form, setForm] = useState<{
    documentType: DocumentType;
    documentNumber: string;
    name: string;
    contactPerson: string;
    email: string;
    phone: string;
    address: string;
    paymentTermDays: string;
  }>({
    documentType: DocumentType.DNI,
    documentNumber: '',
    name: '',
    contactPerson: '',
    email: '',
    phone: '',
    address: '',
    paymentTermDays: '0',
  });
  const [formError, setFormError] = useState('');

  const { data, isLoading, isError, error } = useSuppliers(page, PAGE_SIZE);
  const createMutation = useCreateSupplier();
  const updateMutation = useUpdateSupplier();
  const deleteMutation = useDeleteSupplier();

  const suppliers = data?.items ?? [];

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return suppliers;
    return suppliers.filter(
      (s) =>
        s.name.toLowerCase().includes(term) ||
        s.documentNumber.toLowerCase().includes(term),
    );
  }, [suppliers, search]);

  const openCreate = () => {
    setEditing(null);
    setForm({
      documentType: DocumentType.DNI,
      documentNumber: '',
      name: '',
      contactPerson: '',
      email: '',
      phone: '',
      address: '',
      paymentTermDays: '0',
    });
    setFormError('');
    setModalOpen(true);
  };

  const openEdit = (supplier: Supplier) => {
    setEditing(supplier);
    setForm({
      documentType: supplier.documentType,
      documentNumber: supplier.documentNumber,
      name: supplier.name,
      contactPerson: supplier.contactPerson ?? '',
      email: supplier.email ?? '',
      phone: supplier.phone ?? '',
      address: supplier.address ?? '',
      paymentTermDays: String(supplier.paymentTermDays),
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
    const payload: SupplierPayload = {
      documentType: form.documentType,
      documentNumber: form.documentNumber.trim(),
      name: form.name.trim(),
      contactPerson: form.contactPerson.trim() || null,
      email: form.email.trim() || null,
      phone: form.phone.trim() || null,
      address: form.address.trim() || null,
      paymentTermDays: Number(form.paymentTermDays) || 0,
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

  const handleDelete = async (supplier: Supplier) => {
    if (!window.confirm(`¿Eliminar el proveedor "${supplier.name}"?`)) return;
    try {
      await deleteMutation.mutateAsync(supplier.id);
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
        <h1 className="text-2xl font-bold text-gray-800">Proveedores</h1>
        <Button onClick={openCreate}>Nuevo Proveedor</Button>
      </div>

      <Input
        placeholder="Buscar por nombre o número de documento..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />

      <Table<Supplier>
        rowKey={(s) => s.id}
        columns={[
          {
            header: 'Documento',
            accessor: (s) => `${DOCUMENT_TYPE_LABELS[s.documentType]} ${s.documentNumber}`,
          },
          { header: 'Nombre', accessor: (s) => s.name },
          { header: 'Persona de Contacto', accessor: (s) => s.contactPerson ?? '—' },
          { header: 'Teléfono', accessor: (s) => s.phone ?? '—' },
          {
            header: 'Plazo de Pago',
            accessor: (s) => `${s.paymentTermDays} días`,
          },
          {
            header: 'Estado',
            accessor: (s) =>
              s.isActive ? (
                <span className="text-green-700 font-medium">Activo</span>
              ) : (
                <span className="text-red-600 font-medium">Inactivo</span>
              ),
          },
          {
            header: 'Acciones',
            accessor: (s) => (
              <div className="flex gap-2">
                <Button variant="secondary" onClick={() => openEdit(s)}>
                  Editar
                </Button>
                <Button variant="danger" onClick={() => handleDelete(s)}>
                  Eliminar
                </Button>
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
        title={editing ? 'Editar Proveedor' : 'Nuevo Proveedor'}
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
            label="Persona de Contacto (opcional)"
            value={form.contactPerson}
            onChange={(e) => setForm((f) => ({ ...f, contactPerson: e.target.value }))}
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

          <Input
            label="Plazo de Pago (días)"
            type="number"
            value={form.paymentTermDays}
            onChange={(e) => setForm((f) => ({ ...f, paymentTermDays: e.target.value }))}
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
  if (message.includes('already exists')) {
    return 'Ya existe un proveedor con este documento.';
  }
  if (message.toLowerCase().includes('document number format')) {
    return 'El formato del número de documento no es válido para el tipo seleccionado.';
  }
  if (message.toLowerCase().includes('payment term days cannot be negative')) {
    return 'El plazo de pago no puede ser negativo.';
  }
  return message;
}
