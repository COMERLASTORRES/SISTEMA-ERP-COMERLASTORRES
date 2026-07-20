import { useQuery } from '@tanstack/react-query';
import { reportsApi, type SalesByPeriodFilters } from '../api/reports';

const KEY = 'reports';

export function useSalesByPeriodReport(filters: SalesByPeriodFilters) {
  return useQuery({
    queryKey: [KEY, 'sales-by-period', filters],
    queryFn: async () => {
      const { data } = await reportsApi.getSalesByPeriod(filters);
      return data;
    },
  });
}
