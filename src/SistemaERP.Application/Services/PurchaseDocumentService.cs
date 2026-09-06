using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services;

/// <summary>
/// Genera un PDF profesional para un comprobante de compra, usando QuestPDF 2026.x.
/// Los datos se obtienen directamente de la entidad Purchase (snapshot histórico de costo).
/// </summary>
public class PurchaseDocumentService : IPurchaseDocumentService
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IProductRepository _productRepository;

    public PurchaseDocumentService(
        IPurchaseRepository purchaseRepository,
        ISupplierRepository supplierRepository,
        ITenantRepository tenantRepository,
        IProductRepository productRepository)
    {
        _purchaseRepository = purchaseRepository;
        _supplierRepository = supplierRepository;
        _tenantRepository = tenantRepository;
        _productRepository = productRepository;
    }

    public async Task<byte[]> GeneratePurchaseDocumentPdfAsync(Guid purchaseId)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(purchaseId);
        if (purchase == null)
            throw new InvalidOperationException($"Purchase with Id {purchaseId} not found.");

        var tenant = await _tenantRepository.GetByIdAsync(purchase.TenantId);
        var supplier = await _supplierRepository.GetByIdAsync(purchase.SupplierId);

        var productIds = purchase.Items.Select(i => i.ProductId).Distinct().ToHashSet();
        var products = (await _productRepository.GetAllAsync())
            .Where(p => productIds.Contains(p.Id))
            .ToDictionary(p => p.Id);

        var voucherLabel = purchase.VoucherType switch
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
                page.DefaultTextStyle(x => x.FontSize(10f).FontColor(Colors.Black));

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
                    column.Item().Text($"COMPROBANTE DE COMPRA — {voucherLabel}")
                        .FontSize(14f).Bold();

                    // ---- Datos proveedor ----
                    column.Item().Text("Datos del proveedor").FontSize(10f).Bold();
                    column.Item().Text($"Nombre: {supplier?.Name ?? "—"}");
                    if (supplier != null && !string.IsNullOrEmpty(supplier.DocumentNumber))
                    {
                        column.Item().Text($"Tipo: {supplier.DocumentType}");
                        column.Item().Text($"N° documento: {supplier.DocumentNumber}");
                    }
                    if (supplier != null && !string.IsNullOrEmpty(supplier.Address))
                    {
                        column.Item().Text($"Dirección: {supplier.Address}");
                    }
                    if (supplier != null && !string.IsNullOrEmpty(supplier.ContactPerson))
                    {
                        column.Item().Text($"Contacto: {supplier.ContactPerson}");
                    }

                    // ---- Encabezado documento ----
                    column.Item().Row(row =>
                    {
                        row.AutoItem().Text($"N°: {purchase.PurchaseNumber}");
                        row.AutoItem().Text($"Fecha: {purchase.PurchaseDate:dd/MM/yyyy HH:mm}");
                        row.AutoItem().Text($"Moneda: {purchase.Currency}");
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
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text(t => t.Line("Producto").Bold().FontSize(9f));
                            header.Cell().Text(t => t.Line("Cant.").Bold().FontSize(9f));
                            header.Cell().Text(t => t.Line("Costo Unit.").Bold().FontSize(9f));
                            header.Cell().Text(t => t.Line("Desc. %").Bold().FontSize(9f));
                            header.Cell().Text(t => t.Line("Subtotal").Bold().FontSize(9f));

                            header.Cell().Background(Colors.Grey.Lighten2);
                        });

                        foreach (var item in purchase.Items)
                        {
                            var product = products.GetValueOrDefault(item.ProductId);
                            table.Cell().Text(product?.Name ?? item.ProductId.ToString()).FontSize(9f);
                            table.Cell().Text(item.Quantity.ToString()).FontSize(9f);
                            table.Cell().Text(item.UnitCost.ToString("N2")).FontSize(9f);
                            table.Cell().Text(item.DiscountPercentage.ToString("N2")).FontSize(9f);
                            table.Cell().Text(item.LineSubtotal.ToString("N2")).FontSize(9f);
                        }
                    });

                    // ---- Totales ----
                    column.Item().AlignRight().Column(totals =>
                    {
                        totals.Item().Text($"Subtotal: {purchase.Subtotal:N2}").FontSize(10f);
                        totals.Item().Text($"IGV: {purchase.Tax:N2}").FontSize(10f);
                        totals.Item().Text($"TOTAL: {purchase.Total:N2}").FontSize(12f).Bold();
                    });

                    // ---- Pie ----
                    column.Item().Text("Este documento es un comprobante interno.")
                        .FontSize(8f).FontColor(Colors.Grey.Lighten1);
                });
            });
        });

        return document.GeneratePdf();
    }
}