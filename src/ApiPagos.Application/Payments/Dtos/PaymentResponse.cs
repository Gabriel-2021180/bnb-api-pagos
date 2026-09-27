using ApiPagos.Domain.Payments;

namespace ApiPagos.Application.Payments.Dtos;

public sealed record PaymentResponse(
    Guid PaymentId,
    string ServiceProvider,
    decimal Amount,
    string Status,
    DateTime CreatedAt)
{
    public static PaymentResponse FromDomain(Payment payment) =>
        new(payment.Id, payment.ServiceProvider, payment.Amount, payment.Status, payment.CreatedAt);
}
