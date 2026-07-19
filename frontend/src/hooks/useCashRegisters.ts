import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  cashRegistersApi,
  type CashRegister,
  type OpenCashRegisterPayload,
  type CloseCashRegisterPayload,
  type RegisterCashMovementPayload,
} from '../api/cashRegisters';

const KEY = 'cashRegisters';

// Caja abierta del usuario actual (null si no hay caja).
export function useOpenCashRegister() {
  return useQuery({
    queryKey: [KEY, 'open'],
    queryFn: async () => {
      const { data } = await cashRegistersApi.getOpen();
      return data;
    },
  });
}

export function useCashRegisters(page = 1, pageSize = 10) {
  return useQuery({
    queryKey: [KEY, 'list', page, pageSize],
    queryFn: async () => {
      const { data } = await cashRegistersApi.getAll(page, pageSize);
      return data;
    },
  });
}

export function useCashRegister(id: string | undefined) {
  return useQuery({
    queryKey: [KEY, 'detail', id],
    queryFn: async () => {
      const { data } = await cashRegistersApi.getById(id!);
      return data;
    },
    enabled: !!id,
  });
}

export function useOpenCashRegisterMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: OpenCashRegisterPayload) => cashRegistersApi.open(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useCloseCashRegisterMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: CloseCashRegisterPayload }) =>
      cashRegistersApi.close(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useRegisterCashMovementMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: RegisterCashMovementPayload }) =>
      cashRegistersApi.registerMovement(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export type { CashRegister };
