using ApiPagos.Api.Security;
using ApiPagos.Infrastructure.Messaging;
using ApiPagos.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace ApiPagos.Api.Extensions;

internal static class StartupValidation
{
    /// <summary>
    /// Valida toda la configuración de una vez y reporta cada problema en un mensaje legible,
    /// en lugar de detenerse con un stack trace en el primer error.
    /// </summary>
    public static bool TryValidateConfiguration(this WebApplication app)
    {
        var errors = new List<string>();

        Collect<SecurityOptions>(app.Services, errors);
        Collect<DatabaseOptions>(app.Services, errors);
        Collect<KafkaOptions>(app.Services, errors);

        if (errors.Count == 0)
        {
            return true;
        }

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ApiPagos.Startup");
        logger.LogCritical(
            "La API no puede iniciar porque la configuración está incompleta:{NewLine}{Errors}{NewLine}{Hint}",
            Environment.NewLine,
            string.Join(Environment.NewLine, errors.Select(e => $"  - {e}")),
            Environment.NewLine,
            "Levante el proyecto con 'docker compose up -d --build' usando el archivo .env (ver README), " +
            "o defina los valores en appsettings / variables de entorno (ej. Database__Password).");

        return false;
    }

    private static void Collect<TOptions>(IServiceProvider services, List<string> errors)
        where TOptions : class
    {
        try
        {
            _ = services.GetRequiredService<IOptions<TOptions>>().Value;
        }
        catch (OptionsValidationException ex)
        {
            errors.AddRange(ex.Failures.Select(Simplify));
        }
    }

    // "DataAnnotation validation failed for 'X' members: 'Y' with the error: 'mensaje'." -> "mensaje"
    private static string Simplify(string failure)
    {
        const string marker = "with the error: '";
        var start = failure.IndexOf(marker, StringComparison.Ordinal);
        return start < 0 ? failure : failure[(start + marker.Length)..].TrimEnd('.', '\'') + ".";
    }
}
