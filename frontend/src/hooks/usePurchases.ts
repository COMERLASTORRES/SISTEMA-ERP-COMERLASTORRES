import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  purchasesApi,
  type Purchase,
  type PurchaseStatus,
  type PaymentType,
  type PaymentMethod,
  type CreatePurchasePayload,
  type UpdatePurchasePayload,
} from '../api/purchases';

const KEY = 'purchases';

export function usePurchases(
  status?: number,
  supplierId?: string,
  paymentType?: number,
  page = 1,
  pageSize = 10,
) {
  return useQuery({
    queryKey: [KEY, status ?? 'all', supplierId ?? 'all', paymentType ?? 'all', page, pageSize],
    queryFn: async () => {
      const { data } = await purchasesApi.getAll(
        status as PurchaseStatus | undefined,
        supplierId,
        paymentType as PaymentType | undefined,
        page,
        pageSize,
      );
      return data;
    },
  });
}

export function usePurchase(id: string | undefined) {
  return useQuery({
    queryKey: [KEY, 'detail', id],
    queryFn: async () => {
      const { data } = await purchasesApi.getById(id!);
      return data;
    },
    enabled: !!id,
  });
}

export function useCreatePurchase() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreatePurchasePayload) => purchasesApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useUpdatePurchase() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdatePurchasePayload }) =>
      purchasesApi.update(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useConfirmPurchase() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => purchasesApi.confirm(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useCancelPurchase() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string | null }) =>
      purchasesApi.cancel(id, reason),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useRegisterPurchasePayment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, paymentMethod }: { id: string; paymentMethod: PaymentMethod }) =>
      purchasesApi.registerPayment(id, paymentMethod),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useDeletePurchase() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => purchasesApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export type { Purchase, CreatePurchasePayload, UpdatePurchasePayload };
