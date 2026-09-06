import { useMemo, useEffect, useState } from 'react';
import { RequirePermission } from '../components/RequirePermission';
import { PermissionCodes } from '../api/permissionCodes';
import { Planet } from '../components/ui/Planet';
import { OrbitalModuleButton } from '../components/ui/OrbitalModuleButton';
import { useAuth } from '../auth/AuthContext';
import {
  Clock,
  Shield,
  RefreshCw,
  Activity,
  Building2,
  Package,
  Tag,
  Users,
  ShoppingCart,
  Receipt,
  Wallet,
  BarChart3,
  UserCog,
  Settings,
} from 'lucide-react';

export interface ModuleConfig {
  to: string;
  label: string;
  icon: React.ReactNode;
  permission: string;
}

const MODULES: ModuleConfig[] = [
  { to: '/dashboard', label: 'Dashboard', icon: <Activity size={28} />, permission: PermissionCodes.ReportsView },
  { to: '/productos', label: 'Productos', icon: <Package size={28} />, permission: PermissionCodes.ProductsView },
  { to: '/categorias', label: 'Categorías', icon: <Tag size={28} />, permission: PermissionCodes.CategoriesView },
  { to: '/clientes', label: 'Clientes', icon: <Users size={28} />, permission: PermissionCodes.CustomersView },
  { to: '/proveedores', label: 'Proveedores', icon: <Building2 size={28} />, permission: PermissionCodes.SuppliersView },
  { to: '/compras', label: 'Compras', icon: <ShoppingCart size={28} />, permission: PermissionCodes.PurchasesView },
  { to: '/ventas', label: 'Ventas', icon: <Receipt size={28} />, permission: PermissionCodes.SalesView },
  { to: '/caja', label: 'Caja', icon: <Wallet size={28} />, permission: PermissionCodes.CashRegisterView },
  { to: '/stock', label: 'Stock', icon: <BarChart3 size={28} />, permission: PermissionCodes.StockMovementsView },
  { to: '/reportes', label: 'Reportes', icon: <BarChart3 size={28} />, permission: PermissionCodes.ReportsView },
  { to: '/usuarios', label: 'Usuarios', icon: <UserCog size={28} />, permission: PermissionCodes.UsersView },
  { to: '/configuracion', label: 'Configuración', icon: <Settings size={28} />, permission: PermissionCodes.SettingsView },
];

interface OrbitalPosition {
  x: number;
  y: number;
}

function calculateOrbitalPositions(count: number, radius: number): OrbitalPosition[] {
  return Array.from({ length: count }, (_, i) => {
    const angle = (i * 360) / count;
    const radians = (angle * Math.PI) / 180;
    return {
      x: Math.cos(radians) * radius,
      y: Math.sin(radians) * radius,
    };
  });
}

function StatusIndicator({ icon, label, secondary, showDot = false }: { icon: React.ReactNode; label: string; secondary: string; showDot?: boolean }) {
  return (
    <div className="flex items-center gap-2 text-gray-600">
      <div className="text-blue-600">{icon}</div>
      <div>
        <div className="font-medium text-sm text-gray-800">{label}</div>
        <div className="text-xs text-gray-500 flex items-center gap-1">
          {showDot && <span className="w-2 h-2 bg-green-500 rounded-full" />}
          {secondary}
        </div>
      </div>
    </div>
  );
}

export function WelcomePage() {
  const { user } = useAuth();
  const positions = useMemo(() => calculateOrbitalPositions(MODULES.length, 360), []);
  const [now, setNow] = useState(new Date());

  // Static white particles for subtle background decoration
  const particles = useMemo(() => Array.from({ length: 12 }, (_, i) => ({
    id: i,
    left: Math.random() * 100,
    top: Math.random() * 100,
    size: Math.random() * 1 + 0.5,
  })), []);

  useEffect(() => {
    const timer = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(timer);
  }, []);

  return (
    <div className="h-[calc(100vh-4rem)] w-full relative overflow-hidden flex flex-col">
      {/* Fondo degradado premium */}
      <div className="absolute inset-0 bg-gradient-to-br from-gray-100 via-gray-200 to-gray-300" />

      {/* World map watermark - centered on planet position, low opacity */}
      <div
        className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 opacity-[0.15] pointer-events-none"
        style={{
          width: '100%',
          height: '100%',
          backgroundImage: 'url(/WORLDMAP.png)',
          backgroundRepeat: 'no-repeat',
          backgroundPosition: 'center',
          backgroundSize: 'cover',
        }}
      />

      {/* White particles - subtle fixed dots */}
      {particles.map((p) => (
        <div
          key={`particle-${p.id}`}
          className="absolute rounded-full bg-white/20 pointer-events-none"
          style={{
            left: `${p.left}%`,
            top: `${p.top}%`,
            width: `${p.size}px`,
            height: `${p.size}px`,
          }}
        />
      ))}

      {/* Header de bienvenida */}
      <div className="text-center mb-10 z-10">
        <span className="text-blue-600 text-sm font-medium">Bienvenido a tu centro de control</span>
        <h2 className="text-4xl font-extrabold mt-2">
          <span className="text-gray-800">Sistema</span>
          <span className="text-blue-600">ERP</span>
        </h2>
        <p className="text-lg text-gray-500 mt-3">Gestiona, analiza y hace crecer tu empresa</p>
      </div>

      {/* Orbital modules */}
      {positions.map((pos, index) => {
        const module = MODULES[index];
        return (
          <div
            key={module.to}
            className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 pointer-events-auto"
            style={{
              transform: `translate(calc(-50% + ${pos.x}px), calc(-50% + ${pos.y}px))`,
            }}
          >
            <RequirePermission codes={module.permission}>
              <OrbitalModuleButton to={module.to} icon={module.icon} label={module.label} />
            </RequirePermission>
          </div>
        );
      })}

      {/* Radial glow behind planet - draws attention to center */}
      <div
        className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 pointer-events-none"
        style={{
          width: '600px',
          height: '600px',
          background: 'radial-gradient(circle, rgba(255,255,255,0.4) 0%, rgba(255,255,255,0) 70%)',
        }}
      />

      {/* Concentric circles around planet - subtle rings */}
      <div
        className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 pointer-events-none rounded-full border border-white/5"
        style={{ width: '520px', height: '520px' }}
      />
      <div
        className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 pointer-events-none rounded-full border border-white/3"
        style={{ width: '540px', height: '540px' }}
      />

      {/* Centered planet */}
      <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 z-10">
        <Planet size={500} />
      </div>

      {/* Barra inferior de estado */}
      <div className="mt-auto mb-6 bg-white/80 backdrop-blur-md border-t border-gray-200 pt-4 pb-4">
        <div className="flex items-center justify-center gap-10 flex-wrap">
          <StatusIndicator icon={<Shield size={18} />} label="Seguridad" secondary="Sistema protegido" showDot />
          <StatusIndicator icon={<RefreshCw size={18} />} label="Sincronización" secondary="En tiempo real" showDot />
          <StatusIndicator icon={<Clock size={18} />} label={now.toLocaleTimeString('es-PE')} secondary={now.toLocaleDateString('es-PE')} />
          <StatusIndicator icon={<Activity size={18} />} label="Estado" secondary="Todos los servicios activos" showDot />
          <StatusIndicator icon={<Building2 size={18} />} label="Empresa" secondary={user?.tenantId ? 'Tenant conectado' : '—'} />
        </div>
      </div>
    </div>
  );
}