using ApiPagos.Application.Abstractions;
using ApiPagos.Application.Payments;
using ApiPagos.Application.Payments.Dtos;
using ApiPagos.Application.Payments.Events;
using ApiPagos.Application.Payments.Validators;
using ApiPagos.Domain.Payments;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ApiPagos.UnitTests.Application;

public class PaymentServiceTests
{
    private static readonly DateTimeOffset Now = new(2025, 7, 17, 8, 30, 0, TimeSpan.Zero);

    private readonly IPaymentRepository _repository = Substitute.For<IPaymentRepository>();
    private readonly IPaymentEventPublisher _publisher = Substitute.For<IPaymentEventPublisher>();
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _repository.AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Payment>());

        _service = new PaymentService(
            _repository,
            _publisher,
            new CreatePaymentRequestValidator(),
            new GetPaymentsQueryValidator(),
            new FakeTimeProvider(Now),
            NullLogger<PaymentService>.Instance);
    }

    [Fact]
    public async Task Register_ValidRequest_SavesPendingPaymentAndPublishesEvent()
    {
        var request = new CreatePaymentRequest(Guid.NewGuid(), "SERVICIOS ELÉCTRICOS S.A.", 120.50m, "BOB");

        var response = await _service.RegisterAsync(request, CancellationToken.None);

        Assert.Equal(PaymentStatus.Pending, response.Status);
        Assert.Equal(120.50m, response.Amount);
        Assert.Equal(Now.UtcDateTime, response.CreatedAt);
        await _repository.Received(1).AddAsync(
            Arg.Is<Payment>(p => p.CustomerId == request.CustomerId && p.Currency == "BOB"),
            Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync(
            Arg.Is<PaymentRegisteredEvent>(e => e.PaymentId == response.PaymentId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_InvalidRequest_ThrowsAndDoesNotSave()
    {
        var request = new CreatePaymentRequest(Guid.NewGuid(), "AGUA", 2000m, "BOB");

        await Assert.ThrowsAsync<ValidationException>(() => _service.RegisterAsync(request, CancellationToken.None));

        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Register_PublisherFails_StillReturnsSavedPayment()
    {
        _publisher.PublishAsync(Arg.Any<PaymentRegisteredEvent>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("broker caído"));

        var request = new CreatePaymentRequest(Guid.NewGuid(), "AGUA", 50m, "BOB");

        var response = await _service.RegisterAsync(request, CancellationToken.None);

        Assert.Equal(PaymentStatus.Pending, response.Status);
    }

    [Fact]
    public async Task GetByCustomer_MissingCustomerId_Throws()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetByCustomerAsync(new GetPaymentsQuery(null), CancellationToken.None));
    }

    [Fact]
    public async Task GetByCustomer_ReturnsMappedPayments()
    {
        var customerId = Guid.NewGuid();
        var stored = Payment.Restore(Guid.NewGuid(), customerId, "AGUA", 80m, "BOB", "pendiente", Now.UtcDateTime);
        _repository.GetByCustomerAsync(customerId, Arg.Any<CancellationToken>()).Returns([stored]);

        var result = await _service.GetByCustomerAsync(new GetPaymentsQuery(customerId), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(stored.Id, item.PaymentId);
        Assert.Equal("pendiente", item.Status);
    }
}
