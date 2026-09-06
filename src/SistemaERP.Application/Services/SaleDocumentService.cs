using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services;

/// <summary>
/// Genera un PDF profesional para un comprobante de venta, usando QuestPDF 2026.x.
/// Los datos se obtienen directamente de la entidad Sale (snapshot histórico de precios,
/// por lo que cambios futuros en los productos no alteran el documento).
/// </summary>
public class SaleDocumentService : ISaleDocumentService
{
    private readonly ISaleRepository _saleRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IProductRepository _productRepository;

    public SaleDocumentService(
        ISaleRepository saleRepository,
        ICustomerRepository customerRepository,
        ITenantRepository tenantRepository,
        IProductRepository productRepository)
    {
        _saleRepository = saleRepository;
        _customerRepository = customerRepository;
        _tenantRepository = tenantRepository;
        _productRepository = productRepository;
    }

    public async Task<byte[]> GenerateSaleDocumentPdfAsync(Guid saleId)
    {
        var sale = await _saleRepository.GetByIdAsync(saleId);
        if (sale == null)
            throw new InvalidOperationException($"Sale with Id {saleId} not found.");

        var tenant = await _tenantRepository.GetByIdAsync(sale.TenantId);
        var customer = await _customerRepository.GetByIdAsync(sale.CustomerId);

        var productIds = sale.Items.Select(i => i.ProductId).Distinct().ToHashSet();
        var products = (await _productRepository.GetAllAsync())
            .Where(p => productIds.Contains(p.Id))
            .ToDictionary(p => p.Id);

        var voucherLabel = sale.VoucherType switch
        {
            VoucherType.Boleta => "BOLETA",
            VoucherType.Factura => "FACTURA",
            VoucherType.NotaCredito => "NOTA DE CRÉDITO",
            VoucherType.Otro => "OTRO",
            _ => "COMPROBANTE"
        };

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Black));

                page.Content().Column(column =>
                {
                    column.Spacing(12);

                    // ---- Encabezado empresa ----
                    column.Item().Text(tenant?.Name ?? "EMPRESA")
                        .FontSize(18f).Bold();
                    if (!string.IsNullOrEmpty(tenant?.RUC))
                    {
                        column.Item().Text($"RUC: {tenant.RUC}")
                            .FontSize(9f).FontColor(Colors.Grey.Lighten1);
                    }

                    // ---- Tipo documento ----
                    column.Item().Text($"COMPROBANTE DE VENTA — {voucherLabel}")
                        .FontSize(14f).Bold();

                    // ---- Datos cliente ----
                    column.Item().Text("Datos del cliente").FontSize(10f).Bold();
                    column.Item().Text($"Nombre: {customer?.Name ?? "Consumidor Final"}");
                    if (customer != null && !string.IsNullOrEmpty(customer.DocumentNumber))
                    {
                        column.Item().Text($"Tipo: {customer.DocumentType}");
                        column.Item().Text($"N° documento: {customer.DocumentNumber}");
                    }
                    if (customer != null && !string.IsNullOrEmpty(customer.Address))
                    {
                        column.Item().Text($"Dirección: {customer.Address}");
                    }
                    if (customer != null && !string.IsNullOrEmpty(customer.Phone))
                    {
                        column.Item().Text($"Teléfono: {customer.Phone}");
                    }

                    // ---- Encabezado documento ----
                    column.Item().Row(row =>
                    {
                        row.AutoItem().Text($"N°: {sale.SaleNumber}");
                        row.AutoItem().Text($"Fecha: {sale.SaleDate:dd/MM/yyyy HH:mm}");
                        row.AutoItem().Text($"Moneda: {sale.Currency}");
                    });

                    // ---- Tabla items ----
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1.5f);
                            cols.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text(t => t.Line("Producto").Bold().FontSize(9f));
                            header.Cell().Text(t => t.Line("Cant.").Bold().FontSize(9f));
                            header.Cell().Text(t => t.Line("Prec. Unit.").Bold().FontSize(9f));
                            header.Cell().Text(t => t.Line("Desc. %").Bold().FontSize(9f));
                            header.Cell().Text(t => t.Line("Subtotal").Bold().FontSize(9f));
                            header.Cell().Text(t => t.Line("IGV").Bold().FontSize(9f));
                            header.Cell().Text(t => t.Line("Total").Bold().FontSize(9f));

                            header.Cell().Background(Colors.Grey.Lighten2);
                        });

                        foreach (var item in sale.Items)
                        {
                            var product = products.GetValueOrDefault(item.ProductId);
                            table.Cell().Text(product?.Name ?? item.ProductId.ToString()).FontSize(9f);
                            table.Cell().Text(item.Quantity.ToString()).FontSize(9f);
                            table.Cell().Text(item.UnitPrice.ToString("N2")).FontSize(9f);
                            table.Cell().Text(item.DiscountPercentage.ToString("N2")).FontSize(9f);
                            table.Cell().Text(item.LineSubtotal.ToString("N2")).FontSize(9f);
                            table.Cell().Text(item.LineTax.ToString("N2")).FontSize(9f);
                            table.Cell().Text(item.LineTotal.ToString("N2")).FontSize(9f);
                        }
                    });

                    // ---- Totales ----
                    column.Item().AlignRight().Column(totals =>
                    {
                        totals.Item().Text($"Subtotal: {sale.Subtotal:N2}").FontSize(10f);
                        totals.Item().Text($"IGV: {sale.Tax:N2}").FontSize(10f);
                        totals.Item().Text($"TOTAL: {sale.Total:N2}").FontSize(12f).Bold();
                    });

                    // ---- Pie ----
                    column.Item().Text("Gracias por su compra. Este documento es un comprobante interno.")
                        .FontSize(8f).FontColor(Colors.Grey.Lighten1);
                });
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Genera un archivo Excel (.xlsx) con la lista de ventas.
    /// Columnas: Número, Fecha, Cliente, Tipo de Pago, Estado, Subtotal, IGV, Total.
    /// </summary>
    public async Task<byte[]> GenerateSalesExcelAsync(IReadOnlyList<Sale> sales)
    {
        // Cargar nombres de clientes en una consulta batch
        var customerIds = sales.Select(s => s.CustomerId).Distinct().ToList();
        var customers = (await _customerRepository.GetAllAsync())
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionary(c => c.Id, c => c.Name);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Ventas");

        // ---- Encabezados ----
        var headers = new[] { "Número", "Fecha", "Cliente", "Tipo de Pago", "Estado", "Subtotal", "IGV", "Total" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // ---- Filas ----
        for (var row = 0; row < sales.Count; row++)
        {
            var sale = sales[row];
            var r = row + 2; // fila 1 = headers

            var customerName = customers.GetValueOrDefault(sale.CustomerId) ?? "—";

            var paymentTypeLabel = sale.PaymentType switch
            {
                PaymentType.Cash => "Contado",
                PaymentType.Credit => $"Crédito ({sale.CreditDays} días)",
                _ => "—"
            };

            var statusLabel = sale.Status switch
            {
                SaleStatus.Draft => "Borrador",
                SaleStatus.Confirmed => "Confirmado",
                SaleStatus.Cancelled => "Cancelado",
                _ => "—"
            };

            worksheet.Cell(r, 1).Value = sale.SaleNumber;
            worksheet.Cell(r, 2).Value = sale.SaleDate.ToString("dd/MM/yyyy");
            worksheet.Cell(r, 3).Value = customerName;
            worksheet.Cell(r, 4).Value = paymentTypeLabel;
            worksheet.Cell(r, 5).Value = statusLabel;
            worksheet.Cell(r, 6).Value = sale.Subtotal;
            worksheet.Cell(r, 7).Value = sale.Tax;
            worksheet.Cell(r, 8).Value = sale.Total;

            // Formato numérico para columnas de dinero
            worksheet.Cell(r, 6).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(r, 7).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(r, 8).Style.NumberFormat.Format = "#,##0.00";
        }

        // Ancho automático de columnas
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
