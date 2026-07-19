import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  usersApi,
  type User,
  type CreateUserPayload,
  type UpdateUserPayload,
  type AssignUserRolesPayload,
} from '../api/users';

const KEY = 'users';

export function useUsers() {
  return useQuery({
    queryKey: [KEY],
    queryFn: async () => {
      const { data } = await usersApi.getAll();
      return data as User[];
    },
  });
}

export function useUser(id: string | undefined) {
  return useQuery({
    queryKey: [KEY, 'detail', id],
    queryFn: async () => {
      const { data } = await usersApi.getById(id!);
      return data as User;
    },
    enabled: !!id,
  });
}

export function useCreateUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateUserPayload) => usersApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useUpdateUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateUserPayload }) =>
      usersApi.update(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useAssignUserRoles() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, roleIds }: { id: string; roleIds: string[] }) =>
      usersApi.assignRoles(id, roleIds),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export type { User, CreateUserPayload, UpdateUserPayload, AssignUserRolesPayload };
