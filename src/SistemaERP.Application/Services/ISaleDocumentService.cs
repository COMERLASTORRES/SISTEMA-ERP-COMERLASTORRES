using System.Threading.Tasks;

namespace SistemaERP.Application.Services;

public interface ISaleDocumentService
{
    /// <summary>
    /// Genera un PDF para un comprobante de venta con toda la información relevante:
    /// - Datos del tenant (nombre, RUC)
    /// - Datos del cliente (nombre, documento, dirección, etc.)
    /// - Encabezado del comprobante (número, fecha, tipo, subtotal, IGV, total)
    /// - Tabla detallada de items (producto, cantidad, precio unitario, descuento, subtotal, IGV, total)
    /// </summary>
    Task<byte[]> GenerateSaleDocumentPdfAsync(Guid saleId);
}