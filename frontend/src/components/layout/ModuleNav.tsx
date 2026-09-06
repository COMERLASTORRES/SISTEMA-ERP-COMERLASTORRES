import { NavLink } from 'react-router-dom';
import { RequirePermission } from '../RequirePermission';
import { PermissionCodes } from '../../api/permissionCodes';

const MODULES = [
  { to: '/', label: 'Dashboard', icon: '📊', permission: null },
  { to: '/productos', label: 'Productos', icon: '📦', permission: PermissionCodes.ProductsView },
  { to: '/categorias', label: 'Categorías', icon: '🏷️', permission: PermissionCodes.CategoriesView },
  { to: '/clientes', label: 'Clientes', icon: '𑗧', permission: PermissionCodes.CustomersView },
  { to: '/proveedores', label: 'Proveedores', icon: '🏢', permission: PermissionCodes.SuppliersView },
  { to: '/compras', label: 'Compras', icon: '🛒', permission: PermissionCodes.PurchasesView },
  { to: '/ventas', label: 'Ventas', icon: '🧾', permission: PermissionCodes.SalesView },
  { to: '/caja', label: 'Caja', icon: '💰', permission: PermissionCodes.CashRegisterView },
  { to: '/stock', label: 'Stock', icon: '📈', permission: PermissionCodes.StockMovementsView },
  // { to: '/reportes', label: 'Reportes', icon: '📊', permission: PermissionCodes.ReportsView },
  { to: '/usuarios', label: 'Usuarios', icon: '👤', permission: PermissionCodes.UsersView },
];

interface ModuleItem {
  to: string;
  label: string;
  icon: string;
  permission: string | null;
}

function ModuleButton({ to, label, icon }: Omit<ModuleItem, 'permission'>) {
  return (
    <NavLink
      to={to}
      end={to === '/'}
      className={({ isActive }) =>
        `flex flex-col items-center justify-center px-3 py-2 rounded-lg transition-all duration-200 ${
          isActive
            ? 'bg-blue-600 text-white shadow-md'
            : 'text-gray-600 hover:bg-gray-100 hover:text-gray-800'
        }`
      }
    >
      <span className="text-xl">{icon}</span>
      <span className="text-xs mt-1 font-medium">{label}</span>
    </NavLink>
  );
}

export function ModuleNav() {
  return (
    <nav className="bg-white border-b border-gray-200 sticky top-0 z-40">
      <div className="max-w-full px-4 py-3">
        <div className="flex flex-wrap items-center justify-center gap-2">
          {MODULES.map((module) => {
            const content = <ModuleButton key={module.to} to={module.to} label={module.label} icon={module.icon} />;
            if (module.permission) {
              return (
                <RequirePermission key={module.to} codes={module.permission}>
                  {content}
                </RequirePermission>
              );
            }
            return <div key={module.to}>{content}</div>;
          })}
        </div>
      </div>
    </nav>
  );
}