using SistemaERP.Domain.Entities;

namespace SistemaERP.Api.Models;

/// <summary>
/// Body para registrar el cobro TOTAL de una venta a crédito (PARTE 1). Solo se
/// indica el método de pago; el monto y la transición de estado los resuelve el
/// servicio. El futuro módulo de Cuentas por Cobrar extenderá esto con pagos
/// parciales y múltiples métodos vía la entidad SalePayment.
/// </summary>
public class RegisterPaymentDto
{
    public PaymentMethod PaymentMethod { get; set; }
}
