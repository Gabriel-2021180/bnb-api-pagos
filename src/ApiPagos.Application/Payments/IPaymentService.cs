using ApiPagos.Application.Payments.Dtos;

namespace ApiPagos.Application.Payments;

public interface IPaymentService
{
    Task<PaymentResponse> RegisterAsync(CreatePaymentRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<PaymentResponse>> GetByCustomerAsync(GetPaymentsQuery query, CancellationToken cancellationToken);
}
