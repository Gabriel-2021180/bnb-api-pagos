using ApiPagos.Domain.Payments;

namespace ApiPagos.Application.Abstractions;

public interface IPaymentRepository
{
    Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken);

    Task<IReadOnlyList<Payment>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken);
}
