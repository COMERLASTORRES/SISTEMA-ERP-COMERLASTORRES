using System;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Reports.StockMovements;

/// <summary>
/// Repositorio de lectura especializado para movimientos de stock. Proporciona IQueryable
/// para proyección directa a DTO con AsNoTracking y LEFT JOIN para resolver nombres
/// (Producto) evitando N+1.
/// </summary>
public interface IStockMovementReportRepository
{
    /// <summary>
    /// Raíz de consulta de movimientos de stock: del tenant (filtro global de EF), sin tracking.
    /// No incluye navegaciones: los nombres se resuelven con LEFT JOIN en el servicio.
    /// </summary>
    IQueryable<StockMovement> Query();

    /// <summary>
    /// Raíz de consulta de productos para resolver ProductName (LEFT JOIN por ProductId).
    /// El filtro global de Product (IsActive && TenantId) restringe por tenant.
    /// </summary>
    IQueryable<Product> ProductsQueryable();
}