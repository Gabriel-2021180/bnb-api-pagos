using ApiPagos.Application.Payments.Dtos;
using ApiPagos.Application.Payments.Validators;

namespace ApiPagos.UnitTests.Application;

public class CreatePaymentRequestValidatorTests
{
    private readonly CreatePaymentRequestValidator _validator = new();

    private static CreatePaymentRequest Valid() =>
        new(Guid.NewGuid(), "SERVICIOS ELÉCTRICOS S.A.", 120.50m, "BOB");

    [Fact]
    public void ValidRequest_Passes()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Fact]
    public void AmountAbove1500_Fails()
    {
        var result = _validator.Validate(Valid() with { Amount = 1500.01m });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePaymentRequest.Amount));
    }

    [Fact]
    public void Dollars_FailWithSpecificMessage()
    {
        var result = _validator.Validate(Valid() with { Currency = "USD" });

        var error = Assert.Single(result.Errors);
        Assert.Contains("dólares", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("EUR")]
    public void MissingOrUnsupportedCurrency_Fails(string? currency)
    {
        var result = _validator.Validate(Valid() with { Currency = currency });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePaymentRequest.Currency));
    }

    [Fact]
    public void MissingCustomerAndProvider_Fail()
    {
        var result = _validator.Validate(Valid() with { CustomerId = Guid.Empty, ServiceProvider = " " });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePaymentRequest.CustomerId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePaymentRequest.ServiceProvider));
    }
}
