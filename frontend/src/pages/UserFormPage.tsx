import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import { RequirePermission } from '../components/RequirePermission';
import { PermissionCodes } from '../api/permissionCodes';
import {
  useUser,
  useCreateUser,
  useUpdateUser,
  useAssignUserRoles,
} from '../hooks/useUsers';
import { useRoles, type RoleSummary } from '../hooks/useRoles';

export function UserFormPage() {
  return (
    <RequirePermission
      codes={PermissionCodes.UsersView}
      fallback={
        <div className="p-6 text-center text-gray-600">
          No tienes permiso para ver este módulo.
        </div>
      }
    >
      <UserFormContent />
    </RequirePermission>
  );
}

function UserFormContent() {
  const { id } = useParams();
  const isEdit = !!id;
  const navigate = useNavigate();

  const { data: user, isLoading: loadingUser, isError: userError, error: userErr } = useUser(id);
  const { data: roles } = useRoles();

  const createMutation = useCreateUser();
  const updateMutation = useUpdateUser();
  const assignMutation = useAssignUserRoles();

  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [isActive, setIsActive] = useState(true);
  // Selección única de rol (por ahora). En edición se precarga con el primer rol asignado.
  const [selectedRoleId, setSelectedRoleId] = useState('');
  const [hydrated, setHydrated] = useState(false);
  const [formError, setFormError] = useState('');

  // Flujo de 2 pasos solo para CREAR: null = Paso 1; string = Paso 2 (id ya creado).
  const [createdUserId, setCreatedUserId] = useState<string | null>(null);

  useEffect(() => {
    if (isEdit && user && !hydrated) {
      setFullName(user.fullName);
      setEmail(user.email);
      setIsActive(user.isActive);
      setSelectedRoleId(user.roles.length > 0 ? user.roles[0].id : '');
      setHydrated(true);
    }
  }, [isEdit, user, hydrated]);

  const roleList = (roles ?? []) as RoleSummary[];

  const handleCreate = async () => {
    setFormError('');
    if (!fullName.trim()) {
      setFormError('El nombre es requerido.');
      return;
    }
    if (!email.trim()) {
      setFormError('El email es requerido.');
      return;
    }
    if (!password) {
      setFormError('La contraseña es requerida.');
      return;
    }
    try {
      // Usa el Id de la respuesta del POST, nunca un estado de React desactualizado.
      const created = await createMutation.mutateAsync({
        email: email.trim(),
        password,
        fullName: fullName.trim(),
      });
      setCreatedUserId(created.data.id);
    } catch (err: any) {
      setFormError(extractError(err));
    }
  };

  const handleFinish = async () => {
    setFormError('');
    if (!createdUserId) return;
    const roleIds = selectedRoleId ? [selectedRoleId] : [];
    try {
      console.log('[DEBUG] assigning role, userId =', createdUserId);
      await assignMutation.mutateAsync({ id: createdUserId, roleIds });
      navigate('/usuarios');
    } catch (err: any) {
      setFormError(extractError(err));
    }
  };

  const handleEditSubmit = async () => {
    setFormError('');
    if (!fullName.trim()) {
      setFormError('El nombre es requerido.');
      return;
    }
    if (!id) return;
    const roleIds = selectedRoleId ? [selectedRoleId] : [];
    try {
      await updateMutation.mutateAsync({
        id,
        payload: { id, fullName: fullName.trim(), isActive },
      });
      // Reemplaza la asignación anterior por el rol seleccionado.
      await assignMutation.mutateAsync({ id, roleIds });
      navigate('/usuarios');
    } catch (err: any) {
      setFormError(extractError(err));
    }
  };

  if (isEdit && loadingUser) return <LoadingSpinner />;
  if (isEdit && userError) return <ErrorMessage message={extractError(userErr)} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">{isEdit ? 'Editar Usuario' : 'Nuevo Usuario'}</h1>
        <Button variant="secondary" onClick={() => navigate('/usuarios')}>
          Volver
        </Button>
      </div>

      {formError && <ErrorMessage message={formError} />}

      {isEdit ? (
        // Edición: nombre/email/password/rol juntos (flujo existente).
        <div className="bg-white rounded-lg shadow p-5 space-y-4">
          <Input label="Nombre" value={fullName} onChange={(e) => setFullName(e.target.value)} />
          <label className="flex items-center gap-2 text-sm text-gray-700">
            <input
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
            />
            Usuario activo
          </label>
          <div>
            <label className="block text-xs text-gray-500 mb-1">Rol</label>
            {roleList.length === 0 ? (
              <p className="text-sm text-red-600">
                Primero debes crear un rol en la sección{' '}
                <Link to="/roles/nueva" className="underline font-medium">
                  Roles
                </Link>
                .
              </p>
            ) : (
              <select
                className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm text-gray-800"
                value={selectedRoleId}
                onChange={(e) => setSelectedRoleId(e.target.value)}
              >
                <option value="">— Sin rol —</option>
                {roleList.map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.name}
                    {r.isSystemRole ? ' (sistema)' : ''}
                  </option>
                ))}
              </select>
            )}
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={() => navigate('/usuarios')}>
              Cancelar
            </Button>
            <RequirePermission codes={PermissionCodes.UsersEdit}>
              <Button onClick={handleEditSubmit} disabled={updateMutation.isPending || assignMutation.isPending}>
                Guardar
              </Button>
            </RequirePermission>
          </div>
        </div>
      ) : createdUserId === null ? (
        // PASO 1 (crear): solo Nombre, Email, Password.
        <div className="bg-white rounded-lg shadow p-5 space-y-4">
          <Input label="Nombre" value={fullName} onChange={(e) => setFullName(e.target.value)} />
          <Input label="Email" value={email} onChange={(e) => setEmail(e.target.value)} />
          <Input
            label="Contraseña"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={() => navigate('/usuarios')}>
              Cancelar
            </Button>
            <RequirePermission codes={PermissionCodes.UsersCreate}>
              <Button onClick={handleCreate} disabled={createMutation.isPending}>
                Crear Usuario
              </Button>
            </RequirePermission>
          </div>
        </div>
      ) : (
        // PASO 2 (crear): confirmación + asignación de rol en la misma pantalla.
        <div className="bg-white rounded-lg shadow p-5 space-y-4">
          <div className="bg-green-50 border border-green-200 text-green-700 rounded-md px-4 py-2 text-sm">
            Usuario creado correctamente.
          </div>
          <div>
            <label className="block text-xs text-gray-500 mb-1">Rol</label>
            {roleList.length === 0 ? (
              <p className="text-sm text-red-600">
                Primero debes crear un rol en la sección{' '}
                <Link to="/roles/nueva" className="underline font-medium">
                  Roles
                </Link>
                .
              </p>
            ) : (
              <select
                className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm text-gray-800"
                value={selectedRoleId}
                onChange={(e) => setSelectedRoleId(e.target.value)}
              >
                <option value="">— Sin rol —</option>
                {roleList.map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.name}
                    {r.isSystemRole ? ' (sistema)' : ''}
                  </option>
                ))}
              </select>
            )}
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={() => navigate('/usuarios')}>
              Omitir
            </Button>
            <RequirePermission codes={PermissionCodes.UsersEdit}>
              <Button onClick={handleFinish} disabled={assignMutation.isPending}>
                Asignar Rol y Finalizar
              </Button>
            </RequirePermission>
          </div>
        </div>
      )}
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
