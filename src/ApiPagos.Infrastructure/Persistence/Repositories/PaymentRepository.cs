using ApiPagos.Application.Abstractions;
using ApiPagos.Domain.Payments;
using ApiPagos.Infrastructure.Persistence.StoredProcedures;

namespace ApiPagos.Infrastructure.Persistence.Repositories;

internal sealed class PaymentRepository(IStoredProcedureExecutor executor) : IPaymentRepository
{
    public async Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["payment_id"] = payment.Id,
            ["customer_id"] = payment.CustomerId,
            ["service_provider"] = payment.ServiceProvider,
            ["amount"] = payment.Amount,
            ["currency"] = payment.Currency,
            ["status"] = payment.Status,
            ["created_at"] = payment.CreatedAt
        };

        var row = await executor
            .QuerySingleAsync<PaymentRow>(ProcedureNames.PaymentCreate, parameters, cancellationToken)
            .ConfigureAwait(false);

        return row.ToDomain();
    }

    public async Task<IReadOnlyList<Payment>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?> { ["customer_id"] = customerId };

        var rows = await executor
            .QueryAsync<PaymentRow>(ProcedureNames.PaymentGetByCustomer, parameters, cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(r => r.ToDomain()).ToList();
    }

    private sealed class PaymentRow
    {
        public Guid PaymentId { get; init; }
        public Guid CustomerId { get; init; }
        public string ServiceProvider { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string Currency { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }

        public Payment ToDomain() =>
            Payment.Restore(PaymentId, CustomerId, ServiceProvider, Amount, Currency, Status, CreatedAt);
    }
}
