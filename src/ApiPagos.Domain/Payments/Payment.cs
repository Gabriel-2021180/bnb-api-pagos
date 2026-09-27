using ApiPagos.Domain.Common;

namespace ApiPagos.Domain.Payments;

public sealed class Payment
{
    private Payment(
        Guid id,
        Guid customerId,
        string serviceProvider,
        decimal amount,
        string currency,
        string status,
        DateTime createdAt)
    {
        Id = id;
        CustomerId = customerId;
        ServiceProvider = serviceProvider;
        Amount = amount;
        Currency = currency;
        Status = status;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public Guid CustomerId { get; }
    public string ServiceProvider { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public string Status { get; }
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Crea un pago nuevo en estado pendiente aplicando las reglas de negocio.
    /// </summary>
    public static Payment Create(
        Guid customerId,
        string serviceProvider,
        decimal amount,
        string currency,
        DateTime createdAtUtc)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("El customerId es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(serviceProvider))
        {
            throw new DomainException("El serviceProvider es obligatorio.");
        }

        var normalizedProvider = serviceProvider.Trim();
        if (normalizedProvider.Length > PaymentRules.ServiceProviderMaxLength)
        {
            throw new DomainException(
                $"El serviceProvider no puede superar {PaymentRules.ServiceProviderMaxLength} caracteres.");
        }

        var normalizedCurrency = NormalizeCurrency(currency);
        if (normalizedCurrency == PaymentRules.DollarCurrency)
        {
            throw new DomainException("No se aceptan pagos en dólares (USD).");
        }

        if (normalizedCurrency != PaymentRules.AllowedCurrency)
        {
            throw new DomainException($"Moneda no permitida. Solo se acepta {PaymentRules.AllowedCurrency}.");
        }

        if (amount <= 0)
        {
            throw new DomainException("El monto debe ser mayor a 0.");
        }

        if (amount > PaymentRules.MaxAmount)
        {
            throw new DomainException($"El monto no puede superar {PaymentRules.MaxAmount} Bs.");
        }

        if (decimal.Round(amount, PaymentRules.AmountScale) != amount)
        {
            throw new DomainException($"El monto admite como máximo {PaymentRules.AmountScale} decimales.");
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new DomainException("La fecha de creación debe estar en UTC.");
        }

        return new Payment(
            Guid.NewGuid(),
            customerId,
            normalizedProvider,
            amount,
            normalizedCurrency,
            PaymentStatus.Pending,
            createdAtUtc);
    }

    /// <summary>
    /// Reconstruye un pago ya persistido (sin volver a validar reglas de creación).
    /// </summary>
    public static Payment Restore(
        Guid id,
        Guid customerId,
        string serviceProvider,
        decimal amount,
        string currency,
        string status,
        DateTime createdAtUtc) =>
        new(id, customerId, serviceProvider, amount, currency, status,
            DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc));

    public static string NormalizeCurrency(string? currency) =>
        (currency ?? string.Empty).Trim().ToUpperInvariant();
}
