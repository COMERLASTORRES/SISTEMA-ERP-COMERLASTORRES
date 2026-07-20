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
  useCategories,
  useCreateCategory,
  useUpdateCategory,
  useDeleteCategory,
  type Category,
  type CategoryPayload,
} from '../hooks/useCategories';

const PAGE_SIZE = 10;

export function CategoriesPage() {
  return (
    <RequirePermission
      codes={PermissionCodes.CategoriesView}
      fallback={
        <div className="p-6 text-center text-gray-600">
          No tienes permiso para ver este módulo.
        </div>
      }
    >
      <CategoriesContent />
    </RequirePermission>
  );
}

function CategoriesContent() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Category | null>(null);
  const [form, setForm] = useState<{ name: string; description: string }>({ name: '', description: '' });
  const [formError, setFormError] = useState('');

  const { data, isLoading, isError, error } = useCategories(page, PAGE_SIZE);
  const createMutation = useCreateCategory();
  const updateMutation = useUpdateCategory();
  const deleteMutation = useDeleteCategory();

  const categories = data?.items ?? [];

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return categories;
    return categories.filter((c) => c.name.toLowerCase().includes(term));
  }, [categories, search]);

  const openCreate = () => {
    setEditing(null);
    setForm({ name: '', description: '' });
    setFormError('');
    setModalOpen(true);
  };

  const openEdit = (category: Category) => {
    setEditing(category);
    setForm({ name: category.name, description: category.description ?? '' });
    setFormError('');
    setModalOpen(true);
  };

  const handleSubmit = async () => {
    setFormError('');
    if (!form.name.trim()) {
      setFormError('El nombre es requerido.');
      return;
    }
    const payload: CategoryPayload = {
      name: form.name.trim(),
      description: form.description.trim() || null,
    };
    try {
      if (editing) {
        await updateMutation.mutateAsync({ id: editing.id, payload });
      } else {
        await createMutation.mutateAsync(payload);
      }
      setModalOpen(false);
    } catch (err: any) {
      setFormError(extractError(err));
    }
  };

  const handleDelete = async (category: Category) => {
    if (!window.confirm(`¿Eliminar la categoría "${category.name}"?`)) return;
    try {
      await deleteMutation.mutateAsync(category.id);
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
        <h1 className="text-2xl font-bold text-gray-800">Categorías</h1>
        <RequirePermission
          codes={PermissionCodes.CategoriesCreate}
          fallback={
            <span className="text-sm text-gray-500">
              No tienes permiso para crear categorías.
            </span>
          }
        >
          <Button onClick={openCreate}>Nueva Categoría</Button>
        </RequirePermission>
      </div>

      <Input
        placeholder="Buscar por nombre..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />

      <Table<Category>
        rowKey={(c) => c.id}
        columns={[
          { header: 'Nombre', accessor: (c) => c.name },
          { header: 'Descripción', accessor: (c) => c.description ?? '—' },
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
                <RequirePermission codes={PermissionCodes.CategoriesEdit}>
                  <Button variant="secondary" onClick={() => openEdit(c)}>
                    Editar
                  </Button>
                </RequirePermission>
                <RequirePermission codes={PermissionCodes.CategoriesDelete}>
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

      <Modal open={modalOpen} title={editing ? 'Editar Categoría' : 'Nueva Categoría'} onClose={() => setModalOpen(false)}>
        <div className="space-y-4">
          {formError && <ErrorMessage message={formError} />}
          <Input
            label="Nombre"
            value={form.name}
            onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
          />
          <Input
            label="Descripción (opcional)"
            value={form.description}
            onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
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
