import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { ErrorMessage } from '../components/ui/ErrorMessage';
import {
  useRole,
  useCreateRole,
  useUpdateRole,
  useAssignRolePermissions,
} from '../hooks/useRoles';
import { usePermissions } from '../hooks/usePermissions';
import type { Permission } from '../api/permissions';
import { translateModule, translatePermission } from '../api/permissionLabels';

export function RoleFormPage() {
  const { id } = useParams();
  const isEdit = !!id;
  const navigate = useNavigate();

  const { data: role, isLoading: loadingRole, isError: roleError, error: roleErr } = useRole(id);
  const { data: allPermissions, isLoading: loadingPerms } = usePermissions();

  const createMutation = useCreateRole();
  const updateMutation = useUpdateRole();
  const assignMutation = useAssignRolePermissions();

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [formError, setFormError] = useState('');
  const [hydrated, setHydrated] = useState(false);

  // En edición, hidratamos el formulario una sola vez cuando llega el rol.
  useEffect(() => {
    if (isEdit && role && !hydrated) {
      setName(role.name);
      setDescription(role.description ?? '');
      setSelected(new Set(role.permissions.map((p) => p.id)));
      setHydrated(true);
    }
  }, [isEdit, role, hydrated]);

  const permissionsByModule = useMemo(() => {
    const map = new Map<string, Permission[]>();
    for (const p of allPermissions ?? []) {
      if (!map.has(p.module)) map.set(p.module, []);
      map.get(p.module)!.push(p);
    }
    return map;
  }, [allPermissions]);

  const toggle = (permissionId: string) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(permissionId)) next.delete(permissionId);
      else next.add(permissionId);
      return next;
    });
  };

  const toggleModule = (module: string, permissionIds: string[], checked: boolean) => {
    setSelected((prev) => {
      const next = new Set(prev);
      for (const pid of permissionIds) {
        if (checked) next.add(pid);
        else next.delete(pid);
      }
      return next;
    });
  };

  const handleSubmit = async () => {
    setFormError('');
    if (!name.trim()) {
      setFormError('El nombre es requerido.');
      return;
    }
    const permissionIds = Array.from(selected);
    try {
      if (isEdit && id) {
        await updateMutation.mutateAsync({
          id,
          payload: { name: name.trim(), description: description.trim() || null },
        });
        await assignMutation.mutateAsync({ id, permissionIds });
      } else {
        const created = await createMutation.mutateAsync({
          name: name.trim(),
          description: description.trim() || null,
        });
        await assignMutation.mutateAsync({ id: created.id, permissionIds });
      }
      navigate('/roles');
    } catch (err: any) {
      setFormError(extractError(err));
    }
  };

  if (isEdit && (loadingRole || loadingPerms)) return <LoadingSpinner />;
  if (isEdit && roleError) return <ErrorMessage message={extractError(roleErr)} />;
  if (!isEdit && loadingPerms) return <LoadingSpinner />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-800">{isEdit ? 'Editar Rol' : 'Nuevo Rol'}</h1>
        <Button variant="secondary" onClick={() => navigate('/roles')}>
          Volver
        </Button>
      </div>

      {formError && <ErrorMessage message={formError} />}

      <div className="bg-white rounded-lg shadow p-5 space-y-4">
        <Input label="Nombre" value={name} onChange={(e) => setName(e.target.value)} />
        <Input
          label="Descripción (opcional)"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />
      </div>

      <div className="bg-white rounded-lg shadow p-5 space-y-3">
        <h2 className="text-lg font-semibold text-gray-800">Permisos</h2>
        {Array.from(permissionsByModule.entries()).map(([module, perms]) => (
          <ModuleSection
            key={module}
            module={module}
            label={translateModule(module)}
            permissions={perms}
            selected={selected}
            onToggle={toggle}
            onToggleModule={toggleModule}
          />
        ))}
      </div>

      <div className="flex justify-end gap-2">
        <Button variant="secondary" onClick={() => navigate('/roles')}>
          Cancelar
        </Button>
        <Button
          onClick={handleSubmit}
          disabled={
            createMutation.isPending ||
            updateMutation.isPending ||
            assignMutation.isPending
          }
        >
          {isEdit ? 'Guardar' : 'Crear'}
        </Button>
      </div>
    </div>
  );
}

function ModuleSection({
  module,
  label,
  permissions,
  selected,
  onToggle,
  onToggleModule,
}: {
  module: string;
  label: string;
  permissions: Permission[];
  selected: Set<string>;
  onToggle: (permissionId: string) => void;
  onToggleModule: (module: string, permissionIds: string[], checked: boolean) => void;
}) {
  const [open, setOpen] = useState(true);
  const ids = permissions.map((p) => p.id);
  const allChecked = ids.every((id) => selected.has(id));
  const someChecked = ids.some((id) => selected.has(id));

  return (
    <div className="border border-gray-200 rounded-md">
      <div className="flex items-center justify-between px-4 py-2 bg-gray-50">
        <button
          type="button"
          onClick={() => setOpen((o) => !o)}
          className="flex items-center gap-2 text-sm font-semibold text-gray-700"
        >
          <span className="text-gray-400">{open ? '▾' : '▸'}</span>
          {label}
        </button>
        <label className="flex items-center gap-1 text-xs text-gray-500 cursor-pointer">
          <input
            type="checkbox"
            checked={allChecked}
            ref={(el) => {
              if (el) el.indeterminate = someChecked && !allChecked;
            }}
            onChange={(e) => onToggleModule(module, ids, e.target.checked)}
          />
          Seleccionar módulo
        </label>
      </div>
      {open && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2 px-4 py-3">
          {permissions.map((p) => (
            <label key={p.id} className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
              <input
                type="checkbox"
                checked={selected.has(p.id)}
                onChange={() => onToggle(p.id)}
              />
              {translatePermission(p.code)}
            </label>
          ))}
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
