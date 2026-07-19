import { NavLink } from 'react-router-dom';

const menuItems = [
  { to: '/', label: 'Dashboard', icon: '📊', end: true },
  { to: '/productos', label: 'Productos', icon: '📦' },
  { to: '/categorias', label: 'Categorías', icon: '🏷️' },
  { to: '/clientes', label: 'Clientes', icon: '👥' },
  { to: '/proveedores', label: 'Proveedores', icon: '🏢' },
  { to: '/compras', label: 'Compras', icon: '🧾' },
  { to: '/ventas', label: 'Ventas', icon: '🧾' },
  { to: '/stock', label: 'Movimientos de Stock', icon: '📈' },
];

export function Sidebar() {
  return (
    <aside className="w-64 bg-sidebar text-gray-200 fixed inset-y-0 left-0 flex flex-col">
      <div className="h-16 flex items-center px-6 text-white font-bold text-lg border-b border-sidebar-hover">
        SistemaERP
      </div>
      <nav className="flex-1 px-3 py-4 space-y-1">
        {menuItems.map((item) => (
          <NavLink
            key={item.to}
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
        ))}
      </nav>
    </aside>
  );
}
