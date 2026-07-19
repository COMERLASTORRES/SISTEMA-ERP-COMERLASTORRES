import { useAuth } from '../auth/AuthContext';

/**
 * Devuelve un predicado para verificar si el usuario autenticado cuenta con uno o
 * varios permisos. Reutilizable para todos los módulos: basta con pasar el código
 * de PermissionCodes del backend (ej. "products.create").
 *
 * - string: true si el usuario tiene ese permiso.
 * - string[]: true si tiene AL MENOS UNO de la lista (OR). Para requerir todos,
 *   combinar varias llamadas con && en el consumidor.
 */
export function useHasPermission() {
  const { permissions } = useAuth();
  const set = new Set(permissions);

  return (codes: string | string[]): boolean => {
    if (Array.isArray(codes)) {
      return codes.some((c) => set.has(c));
    }
    return set.has(codes);
  };
}
