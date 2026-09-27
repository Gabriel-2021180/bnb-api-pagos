using ApiPagos.Application.Payments.Dtos;
using ApiPagos.Domain.Payments;
using FluentValidation;

namespace ApiPagos.Application.Payments.Validators;

public sealed class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("El customerId es obligatorio.");

        RuleFor(x => x.ServiceProvider)
            .NotEmpty().WithMessage("El serviceProvider es obligatorio.")
            .MaximumLength(PaymentRules.ServiceProviderMaxLength)
            .WithMessage($"El serviceProvider no puede superar {PaymentRules.ServiceProviderMaxLength} caracteres.");

        RuleFor(x => x.Currency)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("La moneda (currency) es obligatoria.")
            .Must(c => Payment.NormalizeCurrency(c) != PaymentRules.DollarCurrency)
            .WithMessage("No se aceptan pagos en dólares (USD).")
            .Must(c => Payment.NormalizeCurrency(c) == PaymentRules.AllowedCurrency)
            .WithMessage($"Moneda no permitida. Solo se acepta {PaymentRules.AllowedCurrency}.");

        RuleFor(x => x.Amount)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0.")
            .LessThanOrEqualTo(PaymentRules.MaxAmount)
            .WithMessage($"El monto no puede superar {PaymentRules.MaxAmount} Bs.")
            .Must(a => decimal.Round(a, PaymentRules.AmountScale) == a)
            .WithMessage($"El monto admite como máximo {PaymentRules.AmountScale} decimales.");
    }
}
