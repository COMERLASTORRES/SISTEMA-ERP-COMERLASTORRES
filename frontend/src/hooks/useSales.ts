import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  salesApi,
  type Sale,
  type SaleStatus,
  type CreateSalePayload,
  type UpdateSalePayload,
  type ValidateStockItemPayload,
  type StockValidationResponse,
} from '../api/sales';

const KEY = 'sales';

export function useSales(status?: number, customerId?: string, page = 1, pageSize = 10) {
  return useQuery({
    queryKey: [KEY, status ?? 'all', customerId ?? 'all', page, pageSize],
    queryFn: async () => {
      const { data } = await salesApi.getAll(
        status as SaleStatus | undefined,
        customerId,
        page,
        pageSize,
      );
      return data;
    },
  });
}

export function useSale(id: string | undefined) {
  return useQuery({
    queryKey: [KEY, 'detail', id],
    queryFn: async () => {
      const { data } = await salesApi.getById(id!);
      return data;
    },
    enabled: !!id,
  });
}

export function useCreateSale() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateSalePayload) => salesApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useUpdateSale() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateSalePayload }) =>
      salesApi.update(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useConfirmSale() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => salesApi.confirm(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useCancelSale() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string | null }) =>
      salesApi.cancel(id, reason),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useDeleteSale() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => salesApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useValidateSaleStock() {
  return useMutation<StockValidationResponse, unknown, ValidateStockItemPayload[]>({
    mutationFn: async (items) => {
      const { data } = await salesApi.validateStock(items);
      return data;
    },
  });
}

export type { Sale, CreateSalePayload, UpdateSalePayload };
