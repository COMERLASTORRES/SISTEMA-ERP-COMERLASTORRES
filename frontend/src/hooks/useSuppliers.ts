import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { suppliersApi, type Supplier, type SupplierPayload } from '../api/suppliers';

const KEY = 'suppliers';

export function useSuppliers(page: number, pageSize: number) {
  return useQuery({
    queryKey: [KEY, page, pageSize],
    queryFn: async () => {
      const { data } = await suppliersApi.getAll(page, pageSize);
      return data;
    },
  });
}

export function useCreateSupplier() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: SupplierPayload) => suppliersApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useUpdateSupplier() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: SupplierPayload }) =>
      suppliersApi.update(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useDeleteSupplier() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => suppliersApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export type { Supplier, SupplierPayload };
