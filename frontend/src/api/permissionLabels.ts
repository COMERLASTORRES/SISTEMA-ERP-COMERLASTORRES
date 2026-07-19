// Diccionario de traducción de códigos de permiso (modulo.accion) al español legible.
// Centralizado para evitar strings hardcodeados y mantener consistencia en toda la UI.

export const ACTION_LABELS: Record<string, string> = {
  view: 'Ver',
  create: 'Crear',
  edit: 'Editar',
  delete: 'Eliminar',
  confirm: 'Confirmar',
  cancel: 'Cancelar',
  export: 'Exportar',
  manage: 'Gestionar',
  open: 'Abrir',
  close: 'Cerrar',
  movement: 'Registrar movimiento',
  assign: 'Asignar',
};

export const MODULE_LABELS: Record<string, string> = {
  sales: 'Ventas',
  purchases: 'Compras',
  products: 'Productos',
  categories: 'Categorías',
  customers: 'Clientes',
  suppliers: 'Proveedores',
  cashregister: 'Caja',
  stockmovements: 'Movimientos de Stock',
  users: 'Usuarios',
  roles: 'Roles',
  reports: 'Reportes',
};

// Traduce un permiso individual a partir de su code ("sales.create") o de sus partes.
// Si el módulo o la acción no están en el diccionario, cae back al valor crudo para no
// ocultar información.
export function translatePermission(code: string): string {
  const [module, action] = code.split('.');
  const moduleLabel = MODULE_LABELS[module] ?? module;
  const actionLabel = ACTION_LABELS[action] ?? action;
  return `${actionLabel} ${moduleLabel}`;
}

export function translateModule(module: string): string {
  return MODULE_LABELS[module] ?? module;
}
