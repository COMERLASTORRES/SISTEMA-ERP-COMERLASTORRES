import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { categoriesApi, type Category, type CategoryPayload } from '../api/categories';

const KEY = 'categories';

export function useCategories(page: number, pageSize: number) {
  return useQuery({
    queryKey: [KEY, page, pageSize],
    queryFn: async () => {
      const { data } = await categoriesApi.getAll(page, pageSize);
      return data;
    },
  });
}

export function useCreateCategory() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CategoryPayload) => categoriesApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useUpdateCategory() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: CategoryPayload }) =>
      categoriesApi.update(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useDeleteCategory() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => categoriesApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export type { Category, CategoryPayload };
