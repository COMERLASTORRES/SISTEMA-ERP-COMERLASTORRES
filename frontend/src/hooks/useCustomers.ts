import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { customersApi, type Customer, type CustomerPayload } from '../api/customers';

const KEY = 'customers';

export function useCustomers(page: number, pageSize: number) {
  return useQuery({
    queryKey: [KEY, page, pageSize],
    queryFn: async () => {
      const { data } = await customersApi.getAll(page, pageSize);
      return data;
    },
  });
}

export function useCreateCustomer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CustomerPayload) => customersApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useUpdateCustomer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: CustomerPayload }) =>
      customersApi.update(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useDeleteCustomer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => customersApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export type { Customer, CustomerPayload };
