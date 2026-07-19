import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  rolesApi,
  type Role,
  type RoleSummary,
  type RolePayload,
  type AssignPermissionsPayload,
} from '../api/roles';

const KEY = 'roles';

export function useRoles() {
  return useQuery({
    queryKey: [KEY],
    queryFn: async () => {
      const { data } = await rolesApi.getAll();
      return data as RoleSummary[];
    },
  });
}

export function useRole(id: string | undefined) {
  return useQuery({
    queryKey: [KEY, 'detail', id],
    queryFn: async () => {
      const { data } = await rolesApi.getById(id!);
      return data as Role;
    },
    enabled: !!id,
  });
}

export function useCreateRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: RolePayload) => rolesApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useUpdateRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: RolePayload }) =>
      rolesApi.update(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useDeleteRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => rolesApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useAssignRolePermissions() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, permissionIds }: { id: string; permissionIds: string[] }) =>
      rolesApi.assignPermissions(id, permissionIds),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export type { Role, RoleSummary, RolePayload, AssignPermissionsPayload };
