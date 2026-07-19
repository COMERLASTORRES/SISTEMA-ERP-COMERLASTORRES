import type { ReactNode } from 'react';
import { useHasPermission } from '../hooks/useHasPermission';

interface RequirePermissionProps {
  /** Código(s) de permiso (PermissionCodes del backend). */
  codes: string | string[];
  /** Contenido a renderizar cuando se cumple el permiso. */
  children: ReactNode;
  /** Contenido alternativo cuando no se cumple (opcional). */
  fallback?: ReactNode;
}

/**
 * Renderiza `children` solo si el usuario autenticado tiene el permiso solicitado.
 * Reutilizable en cualquier módulo: basta con envolver botones, acciones o secciones.
 * Semántica OR para listas: se muestra si el usuario tiene AL MENOS UNO de los códigos.
 */
export function RequirePermission({ codes, children, fallback = null }: RequirePermissionProps) {
  const hasPermission = useHasPermission();
  return <>{hasPermission(codes) ? children : fallback}</>;
}
