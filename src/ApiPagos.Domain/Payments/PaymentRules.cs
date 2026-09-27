namespace ApiPagos.Domain.Payments;

public static class PaymentRules
{
    public const decimal MaxAmount = 1500m;
    public const int AmountScale = 2;
    public const int ServiceProviderMaxLength = 150;
    public const string AllowedCurrency = "BOB";
    public const string DollarCurrency = "USD";
}
