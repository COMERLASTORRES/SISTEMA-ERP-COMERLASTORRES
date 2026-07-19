import { useMemo } from 'react';
import { useProducts } from './useProducts';
import { useCustomers } from './useCustomers';
import { useSuppliers } from './useSuppliers';
import { useCategories } from './useCategories';
import { useStockMovements } from './useStockMovements';
import type { Product } from '../api/products';
import type { StockMovement } from '../api/stockMovements';

export interface DashboardStats {
  totalProducts: number;
  totalCustomers: number;
  totalSuppliers: number;
  lowStockProducts: Product[];
  recentMovements: StockMovement[];
  isLoading: boolean;
}

export function useDashboardStats(): DashboardStats {
  // Solo leemos el "total" de cada entidad con pageSize=1 (no traemos registros).
  const productsCount = useProducts(1, 1);
  const customersCount = useCustomers(1, 1);
  const suppliersCount = useSuppliers(1, 1);
  const categoriesCount = useCategories(1, 1);

  // Para stock bajo necesitamos los productos completos.
  const productsFull = useProducts(1, 1000);

  // Últimos 5 movimientos (la API no ordena, ordenamos en cliente por fecha desc).
  const movements = useStockMovements(undefined, 1, 5);

  const lowStockProducts = useMemo(() => {
    const items = productsFull.data?.items ?? [];
    return items.filter((p) => p.stock <= p.stockMinimum);
  }, [productsFull.data]);

  const recentMovements = useMemo(() => {
    const items = movements.data?.items ?? [];
    return [...items]
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 5);
  }, [movements.data]);

  const isLoading =
    productsCount.isLoading ||
    customersCount.isLoading ||
    suppliersCount.isLoading ||
    categoriesCount.isLoading ||
    productsFull.isLoading ||
    movements.isLoading;

  return {
    totalProducts: productsCount.data?.total ?? 0,
    totalCustomers: customersCount.data?.total ?? 0,
    totalSuppliers: suppliersCount.data?.total ?? 0,
    lowStockProducts,
    recentMovements,
    isLoading,
  };
}
