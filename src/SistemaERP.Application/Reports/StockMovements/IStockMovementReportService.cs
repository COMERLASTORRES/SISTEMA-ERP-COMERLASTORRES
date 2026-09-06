using System.Collections.Generic;
using SistemaERP.Domain.Entities;
using SistemaERP.Application.Reports.Dtos;

namespace SistemaERP.Application.Reports.StockMovements;

/// <summary>
/// Servicio especializado de reportes de Movimientos de Stock.
/// </summary>
public interface IStockMovementReportService
{
    /// <summary>
    /// Obtiene todos los movimientos de stock filtrados (sin paginar) para exportación a Excel.
    /// Filtros: dateFrom, dateTo, productId, type.
    /// </summary>
    Task<IReadOnlyList<StockMovementReportItemDto>> GetAllMovementsAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        System.Nullable<Guid> productId,
        System.Nullable<StockMovementType> type);

    /// <summary>
    /// Genera un archivo Excel (.xlsx) con los movimientos de stock.
    /// Columnas: Fecha, Producto, Tipo, Cantidad, Stock Anterior, Stock Nuevo, Motivo, Creado por.
    /// </summary>
    Task<byte[]> GenerateStockMovementsExcelAsync(IReadOnlyList<StockMovementReportItemDto> items);
}