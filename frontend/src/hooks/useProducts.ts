import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { productsApi, type Product, type ProductPayload } from '../api/products';

const KEY = 'products';

export function useProducts(page: number, pageSize: number) {
  return useQuery({
    queryKey: [KEY, page, pageSize],
    queryFn: async () => {
      const { data } = await productsApi.getAll(page, pageSize);
      return data;
    },
  });
}

export function useCreateProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: ProductPayload) => productsApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useUpdateProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload, rowVersion }: { id: string; payload: ProductPayload; rowVersion: string }) =>
      productsApi.update(id, { id, rowVersion, ...payload }),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useDeleteProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => productsApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export type { Product, ProductPayload };
