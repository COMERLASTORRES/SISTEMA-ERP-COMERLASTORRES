import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { Layout } from '../components/layout/Layout';
import { LoginPage } from '../pages/LoginPage';
import { DashboardPage } from '../pages/DashboardPage';
import { CategoriesPage } from '../pages/CategoriesPage';
import { ProductsPage } from '../pages/ProductsPage';
import { CustomersPage } from '../pages/CustomersPage';
import { SuppliersPage } from '../pages/SuppliersPage';
import { StockMovementsPage } from '../pages/StockMovementsPage';
import { PurchasesPage } from '../pages/PurchasesPage';
import { PurchaseFormPage } from '../pages/PurchaseFormPage';
import { PurchaseDetailPage } from '../pages/PurchaseDetailPage';
import { ProtectedRoute } from './ProtectedRoute';

export function AppRoutes() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<ProtectedRoute />}>
          <Route element={<Layout />}>
            <Route path="/" element={<DashboardPage />} />
            <Route path="/categorias" element={<CategoriesPage />} />
            <Route path="/productos" element={<ProductsPage />} />
            <Route path="/clientes" element={<CustomersPage />} />
            <Route path="/proveedores" element={<SuppliersPage />} />
            <Route path="/stock" element={<StockMovementsPage />} />
            <Route path="/compras" element={<PurchasesPage />} />
            <Route path="/compras/nueva" element={<PurchaseFormPage />} />
            <Route path="/compras/:id" element={<PurchaseDetailPage />} />
            <Route path="/compras/:id/editar" element={<PurchaseFormPage />} />
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
