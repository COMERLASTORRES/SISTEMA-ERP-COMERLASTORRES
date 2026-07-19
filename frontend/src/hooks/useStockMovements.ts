import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { stockMovementsApi, type StockMovement, type StockMovementPayload } from '../api/stockMovements';

const KEY = 'stockMovements';

export function useStockMovements(productId: string | undefined, page: number, pageSize: number) {
  return useQuery({
    queryKey: [KEY, productId ?? 'all', page, pageSize],
    queryFn: async () => {
      const { data } = await stockMovementsApi.getAll(productId, page, pageSize);
      return data;
    },
  });
}

export function useCreateStockMovement() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: StockMovementPayload) => stockMovementsApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export type { StockMovement, StockMovementPayload };
