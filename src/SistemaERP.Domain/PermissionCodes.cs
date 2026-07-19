namespace SistemaERP.Domain;

/// <summary>
/// Catálogo centralizado de códigos de permiso. Debe mantenerse en sincronía con los
/// permisos sembrados en PermissionSeed (PARTE 2). Referenciar SIEMPRE estos valores
/// desde el código en lugar de literales de cadena, para evitar dispersión y facilitar
/// el mantenimiento (ej. [Authorize(Policy = PermissionCodes.SalesCreate)]).
///
/// El nombre de la policy de autorización coincide con el código del permiso gracias al
/// IAuthorizationPolicyProvider dinámico (ver PermissionPolicyProvider), de modo que
/// Policy = "sales.create" se resuelve a RequireClaim("permission", "sales.create").
/// </summary>
public static class PermissionCodes
{
    // products
    public const string ProductsView = "products.view";
    public const string ProductsCreate = "products.create";
    public const string ProductsEdit = "products.edit";
    public const string ProductsDelete = "products.delete";
    public const string ProductsConfirm = "products.confirm";
    public const string ProductsCancel = "products.cancel";
    public const string ProductsExport = "products.export";
    public const string ProductsManage = "products.manage";

    // categories
    public const string CategoriesView = "categories.view";
    public const string CategoriesCreate = "categories.create";
    public const string CategoriesEdit = "categories.edit";
    public const string CategoriesDelete = "categories.delete";

    // customers
    public const string CustomersView = "customers.view";
    public const string CustomersCreate = "customers.create";
    public const string CustomersEdit = "customers.edit";
    public const string CustomersDelete = "customers.delete";

    // suppliers
    public const string SuppliersView = "suppliers.view";
    public const string SuppliersCreate = "suppliers.create";
    public const string SuppliersEdit = "suppliers.edit";
    public const string SuppliersDelete = "suppliers.delete";

    // purchases
    public const string PurchasesView = "purchases.view";
    public const string PurchasesCreate = "purchases.create";
    public const string PurchasesEdit = "purchases.edit";
    public const string PurchasesDelete = "purchases.delete";
    public const string PurchasesConfirm = "purchases.confirm";
    public const string PurchasesCancel = "purchases.cancel";
    public const string PurchasesExport = "purchases.export";
    public const string PurchasesManage = "purchases.manage";

    // sales
    public const string SalesView = "sales.view";
    public const string SalesCreate = "sales.create";
    public const string SalesEdit = "sales.edit";
    public const string SalesDelete = "sales.delete";
    public const string SalesConfirm = "sales.confirm";
    public const string SalesCancel = "sales.cancel";
    public const string SalesExport = "sales.export";
    public const string SalesManage = "sales.manage";

    // cashregister
    public const string CashRegisterView = "cashregister.view";
    public const string CashRegisterOpen = "cashregister.open";
    public const string CashRegisterClose = "cashregister.close";
    public const string CashRegisterMovement = "cashregister.movement";

    // stockmovements
    public const string StockMovementsView = "stockmovements.view";
    public const string StockMovementsExport = "stockmovements.export";

    // users
    public const string UsersView = "users.view";
    public const string UsersCreate = "users.create";
    public const string UsersEdit = "users.edit";
    public const string UsersDelete = "users.delete";

    // roles
    public const string RolesView = "roles.view";
    public const string RolesCreate = "roles.create";
    public const string RolesEdit = "roles.edit";
    public const string RolesDelete = "roles.delete";
    public const string RolesAssign = "roles.assign";

    // reports
    public const string ReportsView = "reports.view";
    public const string ReportsExport = "reports.export";
}
