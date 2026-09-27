using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ApiPagos.Infrastructure.Messaging;

internal sealed class KafkaHealthCheck(IProducer<string, string> producer) : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = await Task.Run(() =>
            {
                using var admin = new DependentAdminClientBuilder(producer.Handle).Build();
                return admin.GetMetadata(Timeout);
            }, cancellationToken).ConfigureAwait(false);

            return metadata.Brokers.Count > 0
                ? HealthCheckResult.Healthy()
                : new HealthCheckResult(context.Registration.FailureStatus, "Sin brokers de Kafka disponibles");
        }
#pragma warning disable CA1031 // Cualquier fallo se reporta como no saludable
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Kafka no disponible", ex);
        }
    }
}
