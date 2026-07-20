import type { ReactNode } from 'react';
import { useHasPermission } from '../hooks/useHasPermission';

interface RequirePermissionProps {
  /** Código(s) de permiso (PermissionCodes del backend). */
  codes: string | string[];
  /** Contenido a renderizar cuando se cumple el permiso. */
  children: ReactNode;
  /** Contenido alternativo cuando no se cumple (opcional). */
  fallback?: ReactNode;
  /**
   * Si `true` y `codes` es un arreglo, exige TODOS los códigos (AND) en lugar
   * del comportamiento por defecto (AL MENOS UNO, OR).
   */
  all?: boolean;
}

/**
 * Renderiza `children` solo si el usuario autenticado tiene el permiso solicitado.
 * Reutilizable en cualquier módulo: basta con envolver botones, acciones o secciones.
 * Semántica OR para listas: se muestra si el usuario tiene AL MENOS UNO de los códigos.
 * Con `all` se invierte a AND (requiere todos los códigos del arreglo).
 */
export function RequirePermission({ codes, children, fallback = null, all = false }: RequirePermissionProps) {
  const hasPermission = useHasPermission();
  const allowed = Array.isArray(codes)
    ? all
      ? codes.every((c) => hasPermission(c))
      : hasPermission(codes)
    : hasPermission(codes);
  return <>{allowed ? children : fallback}</>;
}
