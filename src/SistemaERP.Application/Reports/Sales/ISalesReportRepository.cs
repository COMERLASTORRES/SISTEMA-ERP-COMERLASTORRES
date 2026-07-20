using System.Linq;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Reports.Sales;

/// <summary>
/// Repositorio de lectura especializado para el reporte de ventas. Expone los IQueryable
/// necesarios para proyectar (Select) directo a DTO, con AsNoTracking y las raíces para
/// resolver nombres (Customer, Vendor) mediante LEFT JOIN en la consulta, sin N+1.
/// El servicio de reporte construye la consulta (filtros, orden, paginación) encima de
/// estas raíces.
///
/// Se mantiene separado de ISaleRepository para no acoplar la lectura transaccional de
/// ventas (escritura/stock/caja) con la lectura analítica de reportes.
/// </summary>
public interface ISalesReportRepository
{
    /// <summary>
    /// Raíz de consulta de ventas: del tenant (filtro global de EF), sin tracking. No incluye
    /// navegaciones: los nombres se resuelven con LEFT JOIN en el servicio para evitar tocar
    /// la entidad Sale.
    /// </summary>
    IQueryable<Sale> Query();

    /// <summary>
    /// Raíz de consulta de clientes para resolver CustomerName (LEFT JOIN por CustomerId).
    /// El filtro global de Customer (IsActive && TenantId) restringe por tenant.
    /// </summary>
    IQueryable<Customer> CustomersQueryable();

    /// <summary>
    /// Raíz de consulta de usuarios para resolver VendorName (CreatedBy). Sale.CreatedBy es
    /// una FK conceptual sin navegación, así que se hace LEFT JOIN contra esta raíz. El filtro
    /// global de User (IsActive && TenantId) ya restringe por tenant.
    /// </summary>
    IQueryable<User> UsersQueryable();
}
