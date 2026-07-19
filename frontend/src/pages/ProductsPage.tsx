import { useMemo, useState } from 'react';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Table } from '../components/ui/Table';
import { Modal } from '../components/ui/Modal';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import {
  useProducts,
  useCreateProduct,
  useUpdateProduct,
  useDeleteProduct,
  type Product,
  type ProductPayload,
} from '../hooks/useProducts';
import { useCategories, type Category } from '../hooks/useCategories';

const PAGE_SIZE = 10;

interface ProductForm {
  code: string;
  name: string;
  barcode: string;
  purchasePrice: string;
  salePrice: string;
  stockMinimum: string;
  categoryId: string;
}

const EMPTY_FORM: ProductForm = {
  code: '',
  name: '',
  barcode: '',
  purchasePrice: '',
  salePrice: '',
  stockMinimum: '',
  categoryId: '',
};

export function ProductsPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Product | null>(null);
  const [form, setForm] = useState<ProductForm>(EMPTY_FORM);
  const [formError, setFormError] = useState('');

  const { data, isLoading, isError, error } = useProducts(page, PAGE_SIZE);
  const { data: categoriesData } = useCategories(1, 100);
  const createMutation = useCreateProduct();
  const updateMutation = useUpdateProduct();
  const deleteMutation = useDeleteProduct();

  const products = data?.items ?? [];
  const categories: Category[] = categoriesData?.items ?? [];
  const categoryNameById = useMemo(() => {
    const map = new Map<string, string>();
    categories.forEach((c) => map.set(c.id, c.name));
    return map;
  }, [categories]);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return products;
    return products.filter(
      (p) => p.name.toLowerCase().includes(term) || p.code.toLowerCase().includes(term),
    );
  }, [products, search]);

  const openCreate = () => {
    setEditing(null);
    setForm(EMPTY_FORM);
    setFormError('');
    setModalOpen(true);
  };

  const openEdit = (product: Product) => {
    setEditing(product);
    setForm({
      code: product.code,
      name: product.name,
      barcode: product.barcode ?? '',
      purchasePrice: String(product.purchasePrice),
      salePrice: String(product.salePrice),
      stockMinimum: String(product.stockMinimum),
      categoryId: product.categoryId ?? '',
    });
    setFormError('');
    setModalOpen(true);
  };

  const handleSubmit = async () => {
    setFormError('');
    if (!form.code.trim()) {
      setFormError('El código es requerido.');
      return;
    }
    if (!form.name.trim()) {
      setFormError('El nombre es requerido.');
      return;
    }

    const payload: ProductPayload = {
      code: form.code.trim(),
      name: form.name.trim(),
      barcode: form.barcode.trim() || null,
      purchasePrice: Number(form.purchasePrice) || 0,
      salePrice: Number(form.salePrice) || 0,
      stockMinimum: Number(form.stockMinimum) || 0,
      categoryId: form.categoryId || null,
    };

    try {
      if (editing) {
        await updateMutation.mutateAsync({ id: editing.id, payload, rowVersion: editing.rowVersion });
      } else {
        await createMutation.mutateAsync(payload);
      }
      setModalOpen(false);
    } catch (err: any) {
      setFormError(extractError(err));
    }
  };

  const handleDelete = async (product: Product) => {
    if (!window.confirm(`¿Eliminar el producto "${product.name}"?`)) return;
    try {
      await deleteMutation.mutateAsync(product.id);
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
        <h1 className="text-2xl font-bold text-gray-800">Productos</h1>
        <Button onClick={openCreate}>Nuevo Producto</Button>
      </div>

      <Input
        placeholder="Buscar por nombre o código..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />

      <Table<Product>
        rowKey={(p) => p.id}
        columns={[
          { header: 'Código', accessor: (p) => p.code },
          { header: 'Nombre', accessor: (p) => p.name },
          {
            header: 'Categoría',
            accessor: (p) => (p.categoryId ? categoryNameById.get(p.categoryId) ?? '—' : '—'),
          },
          { header: 'Precio Compra', accessor: (p) => p.purchasePrice.toFixed(2) },
          { header: 'Precio Venta', accessor: (p) => p.salePrice.toFixed(2) },
          { header: 'Stock', accessor: (p) => p.stock },
          { header: 'Stock Mín.', accessor: (p) => p.stockMinimum },
          {
            header: 'Estado',
            accessor: (p) =>
              p.isActive ? (
                <span className="text-green-700 font-medium">Activo</span>
              ) : (
                <span className="text-red-600 font-medium">Inactivo</span>
              ),
          },
          {
            header: 'Acciones',
            accessor: (p) => (
              <div className="flex gap-2">
                <Button variant="secondary" onClick={() => openEdit(p)}>
                  Editar
                </Button>
                <Button variant="danger" onClick={() => handleDelete(p)}>
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
        title={editing ? 'Editar Producto' : 'Nuevo Producto'}
        onClose={() => setModalOpen(false)}
      >
        <div className="space-y-4">
          {formError && <ErrorMessage message={formError} />}
          <Input
            label="Código *"
            value={form.code}
            onChange={(e) => setForm((f) => ({ ...f, code: e.target.value }))}
          />
          <Input
            label="Nombre *"
            value={form.name}
            onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
          />
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Categoría (opcional)</label>
            <select
              className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              value={form.categoryId}
              onChange={(e) => setForm((f) => ({ ...f, categoryId: e.target.value }))}
            >
              <option value="">— Sin categoría —</option>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </div>
          <Input
            label="Código de Barras (opcional)"
            value={form.barcode}
            onChange={(e) => setForm((f) => ({ ...f, barcode: e.target.value }))}
          />
          <Input
            label="Precio Compra"
            type="number"
            step="0.01"
            value={form.purchasePrice}
            onChange={(e) => setForm((f) => ({ ...f, purchasePrice: e.target.value }))}
          />
          <Input
            label="Precio Venta"
            type="number"
            step="0.01"
            value={form.salePrice}
            onChange={(e) => setForm((f) => ({ ...f, salePrice: e.target.value }))}
          />
          <Input
            label="Stock Mínimo"
            type="number"
            value={form.stockMinimum}
            onChange={(e) => setForm((f) => ({ ...f, stockMinimum: e.target.value }))}
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
