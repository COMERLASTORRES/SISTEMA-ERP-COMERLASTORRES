import { useAuth } from '../auth/AuthContext';
import { useDashboardStats } from '../hooks/useDashboardStats';
import { useProducts } from '../hooks/useProducts';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { Table } from '../components/ui/Table';
import {
  StockMovementType,
  STOCK_MOVEMENT_TYPE_LABELS,
} from '../api/stockMovements';
import type { StockMovement } from '../api/stockMovements';

const TYPE_BADGE: Record<StockMovementType, string> = {
  [StockMovementType.Entrada]: 'bg-green-100 text-green-700',
  [StockMovementType.Salida]: 'bg-red-100 text-red-700',
  [StockMovementType.Ajuste]: 'bg-blue-100 text-blue-700',
};

export function DashboardPage() {
  const { user } = useAuth();
  const { totalProducts, totalCustomers, totalSuppliers, lowStockProducts, recentMovements, isLoading } =
    useDashboardStats();

  const { data: productsData } = useProducts(1, 1000);
  const productName = (id: string): string =>
    productsData?.items.find((p) => p.id === id)?.name ?? id;

  if (isLoading) return <LoadingSpinner />;

  const lowStockCount = lowStockProducts.length;

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-800">Bienvenido, {user?.email}</h1>

      {/* Tarjetas de estadísticas */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard title="Total Productos" value={totalProducts} />
        <StatCard title="Total Clientes" value={totalCustomers} />
        <StatCard title="Total Proveedores" value={totalSuppliers} />
        <StatCard
          title="Productos con Stock Bajo"
          value={lowStockCount}
          danger={lowStockCount > 0}
        />
      </div>

      {/* Productos con stock bajo */}
      {lowStockCount > 0 && (
        <div>
          <h2 className="text-lg font-semibold text-orange-600 mb-2">
            Productos con stock bajo ({lowStockCount})
          </h2>
          <Table
            rowKey={(p) => p.id}
            columns={[
              { header: 'Producto', accessor: (p) => p.name },
              { header: 'Stock Actual', accessor: (p) => p.stock },
              { header: 'Stock Mínimo', accessor: (p) => p.stockMinimum },
            ]}
            data={lowStockProducts}
          />
        </div>
      )}

      {/* Últimos 5 movimientos de stock */}
      <div>
        <h2 className="text-lg font-semibold text-gray-800 mb-2">Últimos movimientos de stock</h2>
        <Table<StockMovement>
          rowKey={(m) => m.id}
          columns={[
            { header: 'Producto', accessor: (m) => productName(m.productId) },
            {
              header: 'Tipo',
              accessor: (m) => (
                <span className={`px-2 py-1 rounded-full text-xs font-medium ${TYPE_BADGE[m.type]}`}>
                  {STOCK_MOVEMENT_TYPE_LABELS[m.type]}
                </span>
              ),
            },
            { header: 'Cantidad', accessor: (m) => m.quantity },
            {
              header: 'Fecha',
              accessor: (m) =>
                new Date(m.createdAt).toLocaleString('es-PE', {
                  day: '2-digit',
                  month: '2-digit',
                  year: 'numeric',
                  hour: '2-digit',
                  minute: '2-digit',
                }),
            },
          ]}
          data={recentMovements}
        />
      </div>
    </div>
  );
}

function StatCard({
  title,
  value,
  danger = false,
}: {
  title: string;
  value: number;
  danger?: boolean;
}) {
  return (
    <div
      className={`rounded-lg shadow p-5 bg-white border-l-4 ${
        danger ? 'border-orange-500' : 'border-blue-500'
      }`}
    >
      <div className="text-sm text-gray-500">{title}</div>
      <div className={`text-3xl font-bold ${danger ? 'text-orange-600' : 'text-gray-800'}`}>
        {value}
      </div>
    </div>
  );
}
