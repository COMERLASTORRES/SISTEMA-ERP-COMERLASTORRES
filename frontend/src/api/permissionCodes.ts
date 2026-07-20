/**
 * Espejo del catálogo PermissionCodes del backend (SistemaERP.Domain). Usar SIEMPRE
 * estas constantes en lugar de literales de cadena para evitar dispersión y mantener
 * la sincronía con el seed y el PermissionPolicyProvider dinámico.
 *
 * El valor de cada constante es el mismo código ("modulo.accion") que emite el backend
 * en el claim "permission" y que valida cada [Authorize(Policy = ...)].
 */
export const PermissionCodes = {
  // products
  ProductsView: 'products.view',
  ProductsCreate: 'products.create',
  ProductsEdit: 'products.edit',
  ProductsDelete: 'products.delete',
  ProductsConfirm: 'products.confirm',
  ProductsCancel: 'products.cancel',
  ProductsExport: 'products.export',
  ProductsManage: 'products.manage',

  // categories
  CategoriesView: 'categories.view',
  CategoriesCreate: 'categories.create',
  CategoriesEdit: 'categories.edit',
  CategoriesDelete: 'categories.delete',

  // customers
  CustomersView: 'customers.view',
  CustomersCreate: 'customers.create',
  CustomersEdit: 'customers.edit',
  CustomersDelete: 'customers.delete',

  // suppliers
  SuppliersView: 'suppliers.view',
  SuppliersCreate: 'suppliers.create',
  SuppliersEdit: 'suppliers.edit',
  SuppliersDelete: 'suppliers.delete',

  // purchases
  PurchasesView: 'purchases.view',
  PurchasesCreate: 'purchases.create',
  PurchasesEdit: 'purchases.edit',
  PurchasesDelete: 'purchases.delete',
  PurchasesConfirm: 'purchases.confirm',
  PurchasesCancel: 'purchases.cancel',
  PurchasesExport: 'purchases.export',
  PurchasesManage: 'purchases.manage',

  // sales
  SalesView: 'sales.view',
  SalesCreate: 'sales.create',
  SalesEdit: 'sales.edit',
  SalesDelete: 'sales.delete',
  SalesConfirm: 'sales.confirm',
  SalesCancel: 'sales.cancel',
  SalesExport: 'sales.export',
  SalesManage: 'sales.manage',

  // cashregister
  CashRegisterView: 'cashregister.view',
  CashRegisterOpen: 'cashregister.open',
  CashRegisterClose: 'cashregister.close',
  CashRegisterMovement: 'cashregister.movement',

  // stockmovements
 StockMovementsView: 'stockmovements.view',
  StockMovementsCreate: 'stockmovements.create',
  StockMovementsExport: 'stockmovements.export',

  // users
  UsersView: 'users.view',
  UsersCreate: 'users.create',
  UsersEdit: 'users.edit',
  UsersDelete: 'users.delete',

  // roles
  RolesView: 'roles.view',
  RolesCreate: 'roles.create',
  RolesEdit: 'roles.edit',
  RolesDelete: 'roles.delete',
  RolesAssign: 'roles.assign',

  // reports
  ReportsView: 'reports.view',
  ReportsExport: 'reports.export',
} as const;
