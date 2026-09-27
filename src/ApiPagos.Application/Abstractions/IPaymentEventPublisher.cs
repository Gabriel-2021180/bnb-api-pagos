using ApiPagos.Application.Payments.Events;

namespace ApiPagos.Application.Abstractions;

public interface IPaymentEventPublisher
{
    Task PublishAsync(PaymentRegisteredEvent paymentEvent, CancellationToken cancellationToken);
}
