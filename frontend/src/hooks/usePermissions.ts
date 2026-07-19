import { useQuery } from '@tanstack/react-query';
import { permissionsApi, type Permission } from '../api/permissions';

const KEY = 'permissions';

export function usePermissions() {
  return useQuery({
    queryKey: [KEY],
    queryFn: async () => {
      const { data } = await permissionsApi.getAll();
      return data as Permission[];
    },
  });
}
