import { Outlet } from 'react-router-dom';
import { Topbar } from './Topbar';

export function LayoutNoPadding() {
  return (
    <div className="min-h-screen bg-gray-100">
      <Topbar />
      <Outlet />
    </div>
  );
}