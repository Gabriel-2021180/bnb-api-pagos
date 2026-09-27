using ApiPagos.Infrastructure.Persistence.StoredProcedures;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ApiPagos.Infrastructure.Persistence;

internal sealed class DatabaseHealthCheck(IStoredProcedureExecutor executor) : IHealthCheck
{
    private static readonly Dictionary<string, object?> NoParameters = [];

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await executor.QuerySingleAsync<int>(ProcedureNames.HealthCheck, NoParameters, cancellationToken)
                .ConfigureAwait(false);
            return HealthCheckResult.Healthy();
        }
#pragma warning disable CA1031 // Cualquier fallo se reporta como no saludable
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Base de datos no disponible", ex);
        }
    }
}
