import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { Table } from '../components/ui/Table';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import { RequirePermission } from '../components/RequirePermission';
import { PermissionCodes } from '../api/permissionCodes';
import { useUsers, type User } from '../hooks/useUsers';
import { RolesPage } from './RolesPage';

type Tab = 'usuarios' | 'roles';

const DENIED = (
  <div className="p-6 text-center text-gray-600">
    No tienes permiso para ver este módulo.
  </div>
);

export function UsersPage() {
  const navigate = useNavigate();
  const { data, isLoading, isError, error } = useUsers();

  const users = data ?? [];
  const [tab, setTab] = useState<Tab>('usuarios');

  const openCreate = () => navigate('/usuarios/nueva');

  if (isLoading) return <LoadingSpinner />;
  if (isError) return <ErrorMessage message={extractError(error)} />;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">Administración</h1>
      </div>

      <div className="flex gap-1 border-b border-gray-200">
        <RequirePermission codes={PermissionCodes.UsersView}>
          <button
            onClick={() => setTab('usuarios')}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px ${
              tab === 'usuarios'
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            Usuarios
          </button>
        </RequirePermission>
        <RequirePermission codes={PermissionCodes.RolesView}>
          <button
            onClick={() => setTab('roles')}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px ${
              tab === 'roles'
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            Roles
          </button>
        </RequirePermission>
      </div>

      <RequirePermission codes={PermissionCodes.UsersView} fallback={DENIED}>
        {tab === 'usuarios' && (
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold text-gray-800">Usuarios</h2>
              <RequirePermission codes={PermissionCodes.UsersCreate}>
                <Button onClick={openCreate}>Nuevo Usuario</Button>
              </RequirePermission>
            </div>

            <Table<User>
              rowKey={(u) => u.id}
              columns={[
                { header: 'Nombre', accessor: (u) => u.fullName },
                { header: 'Email', accessor: (u) => u.email },
                {
                  header: 'Roles',
                  accessor: (u) =>
                    u.roles.length > 0 ? (
                      <span className="text-sm">{u.roles.map((r) => r.name).join(', ')}</span>
                    ) : (
                      <span className="text-gray-400 text-sm">Sin roles</span>
                    ),
                },
                {
                  header: 'Estado',
                  accessor: (u) =>
                    u.isActive ? (
                      <span className="text-green-700 font-medium">Activo</span>
                    ) : (
                      <span className="text-red-600 font-medium">Inactivo</span>
                    ),
                },
                {
                  header: 'Acciones',
                  accessor: (u) => (
                    <RequirePermission codes={PermissionCodes.UsersEdit}>
                      <Button variant="secondary" onClick={() => navigate(`/usuarios/${u.id}/editar`)}>
                        Editar
                      </Button>
                    </RequirePermission>
                  ),
                },
              ]}
              data={users}
            />
          </div>
        )}
      </RequirePermission>

      <RequirePermission codes={PermissionCodes.RolesView} fallback={DENIED}>
        {tab === 'roles' && <RolesPage />}
      </RequirePermission>
    </div>
  );
}

function extractError(err: any): string {
  if (err?.response?.data) {
    if (typeof err.response.data === 'string') return err.response.data;
    if (typeof err.response.data.message === 'string') return err.response.data.message;
  }
  return 'Ocurrió un error inesperado.';
}
