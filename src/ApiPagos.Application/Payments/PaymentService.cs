using ApiPagos.Application.Abstractions;
using ApiPagos.Application.Payments.Dtos;
using ApiPagos.Application.Payments.Events;
using ApiPagos.Domain.Payments;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPagos.Application.Payments;

internal sealed class PaymentService(
    IPaymentRepository repository,
    IPaymentEventPublisher eventPublisher,
    IValidator<CreatePaymentRequest> createValidator,
    IValidator<GetPaymentsQuery> queryValidator,
    TimeProvider timeProvider,
    ILogger<PaymentService> logger) : IPaymentService
{
    public async Task<PaymentResponse> RegisterAsync(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var payment = Payment.Create(request.CustomerId, request.ServiceProvider!, request.Amount, request.Currency!, now);

        var saved = await repository.AddAsync(payment, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Pago {PaymentId} registrado para el cliente {CustomerId} por {Amount} {Currency}",
            saved.Id, saved.CustomerId, saved.Amount, saved.Currency);

        await PublishSafelyAsync(saved, cancellationToken).ConfigureAwait(false);

        return PaymentResponse.FromDomain(saved);
    }

    public async Task<IReadOnlyList<PaymentResponse>> GetByCustomerAsync(GetPaymentsQuery query, CancellationToken cancellationToken)
    {
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken).ConfigureAwait(false);

        var payments = await repository.GetByCustomerAsync(query.CustomerId!.Value, cancellationToken).ConfigureAwait(false);

        return payments.Select(PaymentResponse.FromDomain).ToList();
    }

    // El pago ya quedó persistido: un fallo del broker no debe revertir el registro.
    private async Task PublishSafelyAsync(Payment payment, CancellationToken cancellationToken)
    {
        try
        {
            var paymentEvent = PaymentRegisteredEvent.FromDomain(payment, timeProvider.GetUtcNow().UtcDateTime);
            await eventPublisher.PublishAsync(paymentEvent, cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Se registra el error y se continúa a propósito
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogError(ex, "No se pudo publicar el evento del pago {PaymentId}", payment.Id);
        }
    }
}
