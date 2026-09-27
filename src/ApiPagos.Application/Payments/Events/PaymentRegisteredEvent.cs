using ApiPagos.Domain.Payments;

namespace ApiPagos.Application.Payments.Events;

public sealed record PaymentRegisteredEvent(
    Guid EventId,
    string EventType,
    DateTime OccurredAt,
    Guid PaymentId,
    Guid CustomerId,
    string ServiceProvider,
    decimal Amount,
    string Currency,
    string Status)
{
    public const string Name = "payment.registered";

    public static PaymentRegisteredEvent FromDomain(Payment payment, DateTime occurredAtUtc) =>
        new(Guid.NewGuid(), Name, occurredAtUtc, payment.Id, payment.CustomerId, payment.ServiceProvider,
            payment.Amount, payment.Currency, payment.Status);
}
