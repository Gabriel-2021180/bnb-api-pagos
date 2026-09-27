using Microsoft.Data.SqlClient;
using Npgsql;

namespace ApiPagos.Infrastructure.Persistence;

/// <summary>
/// Arma la cadena de conexión a partir de las piezas configuradas en la sección "Database".
/// </summary>
internal static class ConnectionStringFactory
{
    public const int PostgreSqlDefaultPort = 5432;
    public const int SqlServerDefaultPort = 1433;

    public static string Build(DatabaseOptions options) => options.Provider switch
    {
        DatabaseProvider.PostgreSql => BuildPostgreSql(options),
        DatabaseProvider.SqlServer => BuildSqlServer(options),
        _ => throw new NotSupportedException($"Proveedor de base de datos no soportado: {options.Provider}")
    };

    private static string BuildPostgreSql(DatabaseOptions options)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = options.Host,
            Port = options.Port ?? PostgreSqlDefaultPort,
            Database = options.Name,
            Username = options.Username,
            Password = options.Password,
            Pooling = options.Pooling,
            MaxPoolSize = options.MaxPoolSize,
            Timeout = options.ConnectTimeoutSeconds,
            CommandTimeout = options.CommandTimeoutSeconds,
            ApplicationName = options.ApplicationName,
            SslMode = !options.UseSsl
                ? SslMode.Disable
                : options.TrustServerCertificate ? SslMode.Require : SslMode.VerifyFull
        };

        return builder.ConnectionString;
    }

    private static string BuildSqlServer(DatabaseOptions options)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{options.Host},{options.Port ?? SqlServerDefaultPort}",
            InitialCatalog = options.Name,
            UserID = options.Username,
            Password = options.Password,
            Pooling = options.Pooling,
            MaxPoolSize = options.MaxPoolSize,
            ConnectTimeout = options.ConnectTimeoutSeconds,
            CommandTimeout = options.CommandTimeoutSeconds,
            ApplicationName = options.ApplicationName,
            Encrypt = options.UseSsl ? SqlConnectionEncryptOption.Mandatory : SqlConnectionEncryptOption.Optional,
            TrustServerCertificate = options.TrustServerCertificate
        };

        return builder.ConnectionString;
    }
}
