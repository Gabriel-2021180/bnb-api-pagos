using ApiPagos.Application.Payments.Dtos;
using FluentValidation;

namespace ApiPagos.Application.Payments.Validators;

public sealed class GetPaymentsQueryValidator : AbstractValidator<GetPaymentsQuery>
{
    public GetPaymentsQueryValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotNull().WithMessage("El parámetro customerId es obligatorio.")
            .NotEqual(Guid.Empty).WithMessage("El parámetro customerId no es válido.");
    }
}
