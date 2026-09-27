using ApiPagos.Application.Abstractions;
using ApiPagos.Infrastructure.Messaging;
using ApiPagos.Infrastructure.Persistence;
using ApiPagos.Infrastructure.Persistence.Repositories;
using ApiPagos.Infrastructure.Persistence.StoredProcedures;
using Confluent.Kafka;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiPagos.Infrastructure;

public static class DependencyInjection
{
    public const string ReadyTag = "ready";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddSingleton<IStoredProcedureExecutor, StoredProcedureExecutor>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        var healthChecks = services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", HealthStatus.Unhealthy, [ReadyTag]);

        AddMessaging(services, configuration, healthChecks);

        return services;
    }

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration, IHealthChecksBuilder healthChecks)
    {
        var section = configuration.GetSection(KafkaOptions.SectionName);
        services.AddOptions<KafkaOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .Validate(o => !o.Enabled || !string.IsNullOrWhiteSpace(o.BootstrapServers),
                "Kafka:BootstrapServers es obligatorio cuando Kafka:Enabled es true.")
            .ValidateOnStart();

        if (!section.GetValue<bool>(nameof(KafkaOptions.Enabled)))
        {
            services.AddSingleton<IPaymentEventPublisher, NullPaymentEventPublisher>();
            return;
        }

        services.AddSingleton(sp => KafkaProducerFactory.Create(
            sp.GetRequiredService<IOptions<KafkaOptions>>().Value,
            sp.GetRequiredService<ILoggerFactory>().CreateLogger("Kafka")));
        services.AddSingleton<IPaymentEventPublisher, KafkaPaymentEventPublisher>();

        // El broker es una dependencia degradable: la API sigue registrando pagos sin él.
        healthChecks.AddCheck<KafkaHealthCheck>("kafka", HealthStatus.Degraded, [ReadyTag]);
    }
}
