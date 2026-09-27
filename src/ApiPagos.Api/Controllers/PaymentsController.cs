using ApiPagos.Application.Payments;
using ApiPagos.Application.Payments.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ApiPagos.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
public sealed class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    /// <summary>
    /// Registra un pago en estado "pendiente".
    /// </summary>
    /// <remarks>
    /// Reglas: moneda obligatoria y solo "BOB" (se rechaza USD), monto mayor a 0 y hasta 1500 Bs con 2 decimales.
    /// </remarks>
    /// <response code="201">Pago registrado.</response>
    /// <response code="400">Datos inválidos o regla de negocio incumplida.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentResponse>> Create(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var payment = await paymentService.RegisterAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByCustomer), new { customerId = request.CustomerId }, payment);
    }

    /// <summary>
    /// Lista los pagos de un cliente, del más reciente al más antiguo.
    /// </summary>
    /// <param name="customerId">Identificador del cliente.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="200">Pagos del cliente (lista vacía si no tiene).</response>
    /// <response code="400">customerId ausente o inválido.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PaymentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<PaymentResponse>>> GetByCustomer(
        [FromQuery] Guid? customerId,
        CancellationToken cancellationToken)
    {
        var payments = await paymentService.GetByCustomerAsync(new GetPaymentsQuery(customerId), cancellationToken);
        return Ok(payments);
    }
}
