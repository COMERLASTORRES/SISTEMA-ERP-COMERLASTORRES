namespace SistemaERP.Api.Models;

/// <summary>
/// Body para cancelar un documento (Venta/Compra). El motivo es opcional pero se
/// registra para trazabilidad de auditoría cuando se proporciona.
/// </summary>
public class CancelDto
{
    public string? Reason { get; set; }
}
