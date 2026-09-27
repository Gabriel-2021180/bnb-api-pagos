using ApiPagos.Application.Payments;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPagos.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
