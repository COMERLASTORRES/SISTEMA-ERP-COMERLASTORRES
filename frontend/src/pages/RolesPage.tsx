import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { Table } from '../components/ui/Table';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import {
  useRoles,
  useDeleteRole,
  type RoleSummary,
} from '../hooks/useRoles';

export function RolesPage() {
  const navigate = useNavigate();
  const { data, isLoading, isError, error } = useRoles();
  const deleteMutation = useDeleteRole();
  const [formError, setFormError] = useState('');

  const roles = data ?? [];

  const openCreate = () => navigate('/roles/nueva');

  const handleDelete = async (role: RoleSummary) => {
    if (role.isSystemRole) return;
    if (!window.confirm(`¿Eliminar el rol "${role.name}"?`)) return;
    setFormError('');
    try {
      await deleteMutation.mutateAsync(role.id);
    } catch (err: any) {
      setFormError(extractError(err));
    }
  };

  if (isLoading) return <LoadingSpinner />;
  if (isError) return <ErrorMessage message={extractError(error)} />;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">Roles</h1>
        <Button onClick={openCreate}>Nuevo Rol</Button>
      </div>

      {formError && <ErrorMessage message={formError} />}

      <Table<RoleSummary>
        rowKey={(r) => r.id}
        columns={[
          { header: 'Nombre', accessor: (r) => r.name },
          { header: 'Descripción', accessor: (r) => r.description ?? '—' },
          {
            header: 'Permisos',
            accessor: (r) => <span className="font-medium">{r.permissionCount}</span>,
          },
          {
            header: 'Rol de Sistema',
            accessor: (r) =>
              r.isSystemRole ? (
                <span className="px-2 py-1 rounded-full text-xs font-medium bg-purple-100 text-purple-700">
                  Sistema
                </span>
              ) : (
                <span className="px-2 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-600">
                  Personalizado
                </span>
              ),
          },
          {
            header: 'Acciones',
            accessor: (r) => (
              <div className="flex gap-2">
                <Button variant="secondary" onClick={() => navigate(`/roles/${r.id}/editar`)}>
                  Editar
                </Button>
                <Button
                  variant="danger"
                  disabled={r.isSystemRole || deleteMutation.isPending}
                  onClick={() => handleDelete(r)}
                >
                  Eliminar
                </Button>
              </div>
            ),
          },
        ]}
        data={roles}
      />
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
