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
import { SalesPage } from '../pages/SalesPage';
import { SaleFormPage } from '../pages/SaleFormPage';
import { SaleDetailPage } from '../pages/SaleDetailPage';
import { CashRegisterPage } from '../pages/CashRegisterPage';
import { CashRegisterHistoryPage } from '../pages/CashRegisterHistoryPage';
import { CashRegisterDetailPage } from '../pages/CashRegisterDetailPage';
import { RolesPage } from '../pages/RolesPage';
import { RoleFormPage } from '../pages/RoleFormPage';
import { UsersPage } from '../pages/UsersPage';
import { UserFormPage } from '../pages/UserFormPage';
import { SalesReportPage } from '../pages/SalesReportPage';
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
            <Route path="/ventas" element={<SalesPage />} />
            <Route path="/ventas/nueva" element={<SaleFormPage />} />
            <Route path="/ventas/:id" element={<SaleDetailPage />} />
            <Route path="/ventas/:id/editar" element={<SaleFormPage />} />
            <Route path="/caja" element={<CashRegisterPage />} />
            <Route path="/caja/historial" element={<CashRegisterHistoryPage />} />
            <Route path="/caja/:id" element={<CashRegisterDetailPage />} />
            <Route path="/roles" element={<RolesPage />} />
            <Route path="/roles/nueva" element={<RoleFormPage />} />
            <Route path="/roles/:id/editar" element={<RoleFormPage />} />
            <Route path="/usuarios" element={<UsersPage />} />
            <Route path="/usuarios/nueva" element={<UserFormPage />} />
            <Route path="/usuarios/:id/editar" element={<UserFormPage />} />
            <Route path="/reportes" element={<SalesReportPage />} />
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
