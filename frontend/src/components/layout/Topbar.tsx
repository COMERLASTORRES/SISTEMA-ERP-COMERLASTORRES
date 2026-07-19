import { useAuth } from '../../auth/AuthContext';
import { Button } from '../ui/Button';

export function Topbar() {
  const { user, logout } = useAuth();
  return (
    <header className="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-6 sticky top-0 z-10">
      <div className="text-gray-500 text-sm">Panel de control</div>
      <div className="flex items-center gap-4">
        <span className="text-sm text-gray-700 font-medium">{user?.email ?? '—'}</span>
        <Button variant="secondary" onClick={logout}>
          Cerrar sesión
        </Button>
      </div>
    </header>
  );
}
