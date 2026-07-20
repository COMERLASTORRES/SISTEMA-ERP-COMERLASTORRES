using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Reports.Dtos;

/// <summary>
/// Filtros reutilizables para todos los reportes del sistema. Un único DTO sirve
/// como contrato común para Ventas, Compras, Inventario, Caja, Cuentas por Cobrar/Pagar,
/// Dashboard, etc. Cada reporte ignora los campos que no le aplican.
///
/// El servidor aplica siempre paginación (Page/PageSize) y, de forma opcional, ordenamiento
/// (SortBy/SortDirection). Los rangos de fecha y los identificadores de entidad permiten
/// acotar el conjunto de datos antes de proyectar al DTO de salida.
/// </summary>
public class ReportFilterDto
{
    /// <summary>Inicio del rango de fechas (inclusive). Nulo = sin cota inferior.</summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>Fin del rango de fechas (inclusive). Nulo = sin cota superior.</summary>
    public DateTime? DateTo { get; set; }

    /// <summary>Cliente (Ventas, Cuentas por Cobrar).</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>Proveedor (Compras, Cuentas por Pagar).</summary>
    public Guid? SupplierId { get; set; }

    /// <summary>Vendedor/Responsable (CreatedBy). Aplica a Ventas y otros documentos.</summary>
    public Guid? VendorId { get; set; }

    /// <summary>Almacén asociado al documento.</summary>
    public Guid? WarehouseId { get; set; }

    /// <summary>Forma de pago (Cash/Credit).</summary>
    public PaymentType? PaymentType { get; set; }

    /// <summary>Estado de pago (Pending/Partial/Paid).</summary>
    public PaymentStatus? PaymentStatus { get; set; }

    /// <summary>Estado del documento (Draft/Confirmed/Cancelled).</summary>
    public SaleStatus? DocumentStatus { get; set; }

    /// <summary>
    /// Cuando es true, la tabla del reporte NO aplica filtro de estado (muestra todos los
    /// estados). Tiene prioridad sobre DocumentStatus. Cuando es false/ausente y
    /// DocumentStatus es nulo, la tabla usa Confirmadas por defecto. El resumen SIEMPRE
    /// se calcula solo sobre Confirmadas, sin importar este flag.
    /// </summary>
    public bool IncludeAllStatuses { get; set; }

    /// <summary>Búsqueda libre sobre número de documento u otros campos de texto.</summary>
    public string? Search { get; set; }

    /// <summary>Página actual (basado en 1).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Tamaño de página.</summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Campo por el cual ordenar. El servicio de cada reporte mapea este nombre al
    /// miembro real del DTO de salida. Nulo = orden por defecto del reporte.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>Dirección de ordenamiento.</summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Desc;
}

/// <summary>Dirección de ordenamiento para los reportes.</summary>
public enum SortDirection
{
    Asc,
    Desc,
}
