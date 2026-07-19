import { NavLink } from 'react-router-dom';
import { RequirePermission } from '../RequirePermission';
import { PermissionCodes } from '../../api/permissionCodes';

const operationalItems = [
  { to: '/', label: 'Dashboard', icon: '📊', end: true },
  { to: '/productos', label: 'Productos', icon: '📦', permission: PermissionCodes.ProductsView },
  { to: '/categorias', label: 'Categorías', icon: '🏷️', permission: PermissionCodes.CategoriesView },
  { to: '/clientes', label: 'Clientes', icon: '👥', permission: PermissionCodes.CustomersView },
  { to: '/proveedores', label: 'Proveedores', icon: '🏢', permission: PermissionCodes.SuppliersView },
  { to: '/compras', label: 'Compras', icon: '🧾', permission: PermissionCodes.PurchasesView },
  { to: '/ventas', label: 'Ventas', icon: '🧾', permission: PermissionCodes.SalesView },
  { to: '/caja', label: 'Caja', icon: '💰', permission: PermissionCodes.CashRegisterView },
  { to: '/stock', label: 'Movimientos de Stock', icon: '📈', permission: PermissionCodes.StockMovementsView },
];

const adminItems = [
  { to: '/usuarios', label: 'Usuarios', icon: '👤', permission: PermissionCodes.UsersView },
];

interface NavItem {
  to: string;
  label: string;
  icon: string;
  end?: boolean;
  permission?: string;
}

function NavItemLink({ item }: { item: NavItem }) {
  const link = (
    <NavLink
      to={item.to}
      end={item.end}
      className={({ isActive }) =>
        `flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors ${
          isActive ? 'bg-blue-600 text-white' : 'text-gray-300 hover:bg-sidebar-hover'
        }`
      }
    >
      <span className="text-base">{item.icon}</span>
      {item.label}
    </NavLink>
  );

  // Si el item exige un permiso, solo se muestra si el usuario lo tiene.
  if (item.permission) {
    return <RequirePermission codes={item.permission}>{link}</RequirePermission>;
  }
  return link;
}

export function Sidebar() {
  return (
    <aside className="w-64 bg-sidebar text-gray-200 fixed inset-y-0 left-0 flex flex-col">
      <div className="h-16 flex items-center px-6 text-white font-bold text-lg border-b border-sidebar-hover">
        SistemaERP
      </div>
      <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
        {operationalItems.map((item) => (
          <NavItemLink key={item.to} item={item} />
        ))}

        <div className="pt-4 mt-2 border-t border-sidebar-hover">
          <div className="px-3 pb-2 text-xs uppercase tracking-wider text-gray-500">
            Administración
          </div>
          {adminItems.map((item) => (
            <NavItemLink key={item.to} item={item} />
          ))}
        </div>
      </nav>
    </aside>
  );
}
