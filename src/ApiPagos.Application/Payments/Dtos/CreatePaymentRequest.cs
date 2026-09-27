namespace ApiPagos.Application.Payments.Dtos;

/// <summary>
/// Datos para registrar un pago de servicio básico.
/// </summary>
/// <param name="CustomerId">Identificador del cliente.</param>
/// <param name="ServiceProvider">Empresa proveedora del servicio.</param>
/// <param name="Amount">Monto a pagar (mayor a 0, máximo 1500, hasta 2 decimales).</param>
/// <param name="Currency">Moneda del monto. Solo se acepta "BOB".</param>
public sealed record CreatePaymentRequest(
    Guid CustomerId,
    string? ServiceProvider,
    decimal Amount,
    string? Currency);
