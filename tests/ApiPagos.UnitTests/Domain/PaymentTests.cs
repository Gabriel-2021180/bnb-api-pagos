using ApiPagos.Domain.Common;
using ApiPagos.Domain.Payments;

namespace ApiPagos.UnitTests.Domain;

public class PaymentTests
{
    private static readonly Guid CustomerId = Guid.Parse("cfe8b150-2f84-4a1a-bdf4-923b20e34973");
    private static readonly DateTime Now = new(2025, 7, 17, 8, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ValidData_ReturnsPendingPayment()
    {
        var payment = Payment.Create(CustomerId, "  SERVICIOS ELÉCTRICOS S.A. ", 120.50m, "bob", Now);

        Assert.NotEqual(Guid.Empty, payment.Id);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal("SERVICIOS ELÉCTRICOS S.A.", payment.ServiceProvider);
        Assert.Equal("BOB", payment.Currency);
        Assert.Equal(Now, payment.CreatedAt);
    }

    [Fact]
    public void Create_MaxAmount_IsAccepted()
    {
        var payment = Payment.Create(CustomerId, "AGUA", PaymentRules.MaxAmount, "BOB", Now);

        Assert.Equal(1500m, payment.Amount);
    }

    [Theory]
    [InlineData(1500.01)]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(10.555)]
    public void Create_InvalidAmount_Throws(decimal amount)
    {
        Assert.Throws<DomainException>(() => Payment.Create(CustomerId, "AGUA", amount, "BOB", Now));
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("usd")]
    [InlineData("EUR")]
    [InlineData("")]
    public void Create_CurrencyOtherThanBob_Throws(string currency)
    {
        Assert.Throws<DomainException>(() => Payment.Create(CustomerId, "AGUA", 100m, currency, Now));
    }

    [Fact]
    public void Create_EmptyCustomer_Throws()
    {
        Assert.Throws<DomainException>(() => Payment.Create(Guid.Empty, "AGUA", 100m, "BOB", Now));
    }

    [Fact]
    public void Create_NonUtcDate_Throws()
    {
        var local = DateTime.SpecifyKind(Now, DateTimeKind.Local);

        Assert.Throws<DomainException>(() => Payment.Create(CustomerId, "AGUA", 100m, "BOB", local));
    }
}
