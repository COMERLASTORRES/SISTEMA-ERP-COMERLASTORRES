using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Application.Reports.Dtos;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Reports.StockMovements;

/// <summary>
/// Implementación del reporte de movimientos de stock. Consultas proyección (Select)
/// directas a StockMovementReportItemDto sobre IQueryable, con AsNoTracking y
/// LEFT JOIN para resolver nombres de producto.
/// </summary>
public class StockMovementReportService : IStockMovementReportService
{
    private readonly IStockMovementReportRepository _repository;

    public StockMovementReportService(IStockMovementReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<StockMovementReportItemDto>> GetAllMovementsAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        Guid? productId,
        StockMovementType? type)
    {
        var query = _repository.Query();

        // Normalización de fechas: el DateTime que llega del query string se bindea como
        // Kind=Unspecified, y PostgreSQL (timestamp with time zone) solo acepta UTC. Lo
        // forzamos a UTC para evitar el error "Cannot write DateTime with Kind=Unspecified".
        // DateTo se extiende al final del día (23:59:59.999) para incluir el día completo.
        if (dateFrom.HasValue)
        {
            var from = DateTime.SpecifyKind(dateFrom.Value.Date,DateTimeKind.Utc);
            query = query.Where(s => s.CreatedAt >= from);
        }
        if (dateTo.HasValue)
        {
            var to = DateTime.SpecifyKind(
                dateTo.Value.Date.AddDays(1).AddTicks(-1),
               DateTimeKind.Utc);
            query = query.Where(s => s.CreatedAt <= to);
        }
        if (productId.HasValue)
            query = query.Where(s => s.ProductId == productId.Value);
        if (type.HasValue)
            query = query.Where(s => s.Type == type.Value);

        // Orden: fecha descendente por defecto
        var ordered = query.OrderByDescending(s => s.CreatedAt);

        // Proyección con LEFT JOIN a Products (ProductName)
        var projected =
            from movement in ordered
            join product in _repository.ProductsQueryable() on movement.ProductId equals product.Id into productGroup
            from product in productGroup.DefaultIfEmpty()
            select new StockMovementReportItemDto
            {
                CreatedAt = movement.CreatedAt,
                ProductName = product != null ? product.Name : "—",
                Type = movement.Type,
                Quantity = movement.Quantity,
                PreviousStock = movement.PreviousStock,
                NewStock = movement.NewStock,
                Reason = movement.Reason,
                CreatedBy = movement.CreatedBy,
            };

        return await projected.ToListAsync();
    }

    public async Task<byte[]> GenerateStockMovementsExcelAsync(IReadOnlyList<StockMovementReportItemDto> items)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Movs. Stock");

        // ---- Encabezados ----
        var headers = new[]
        {
            "Fecha", "Producto", "Tipo", "Cantidad",
            "Stock Anterior", "Stock Nuevo", "Motivo", "Creado por"
        };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // ---- Filas ----
        for (var row = 0; row < items.Count; row++)
        {
            var item = items[row];
            var r = row + 2;

            var typeLabel = item.Type switch
            {
                StockMovementType.Entrada => "Entrada",
                StockMovementType.Salida => "Salida",
                StockMovementType.Ajuste => "Ajuste",
                _ => "—"
            };

            worksheet.Cell(r, 1).Value = item.CreatedAt.ToString("dd/MM/yyyy HH:mm");
            worksheet.Cell(r, 2).Value = item.ProductName;
            worksheet.Cell(r, 3).Value = typeLabel;
            worksheet.Cell(r, 4).Value = item.Quantity;
            worksheet.Cell(r, 5).Value = item.PreviousStock;
            worksheet.Cell(r, 6).Value = item.NewStock;
            worksheet.Cell(r, 7).Value = item.Reason;
            worksheet.Cell(r, 8).Value = item.CreatedBy ?? "—";
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return await Task.FromResult(stream.ToArray());
    }
}