using System.Threading.Tasks;

namespace SistemaERP.Application.Services;

public interface IPurchaseDocumentService
{
    /// <summary>
    /// Genera un PDF para un comprobante de compra con toda la información relevante:
    /// - Datos del tenant (nombre, RUC)
    /// - Datos del proveedor (nombre, documento, dirección, etc.)
    /// - Encabezado del comprobante (número, fecha, tipo, subtotal, IGV, total)
    /// - Tabla detallada de items (producto, cantidad, precio unitario, descuento, subtotal)
    /// </summary>
    Task<byte[]> GeneratePurchaseDocumentPdfAsync(Guid purchaseId);
}