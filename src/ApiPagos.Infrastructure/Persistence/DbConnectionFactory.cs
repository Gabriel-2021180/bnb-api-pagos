using System.Data.Common;
using ApiPagos.Application.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ApiPagos.Infrastructure.Persistence;

internal interface IDbConnectionFactory
{
    DatabaseProvider Provider { get; }

    Task<DbConnection> OpenAsync(CancellationToken cancellationToken);
}

internal sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IOptions<DatabaseOptions> options)
    {
        Provider = options.Value.Provider;
        _connectionString = ConnectionStringFactory.Build(options.Value);
    }

    public DatabaseProvider Provider { get; }

    public async Task<DbConnection> OpenAsync(CancellationToken cancellationToken)
    {
        DbConnection connection = Provider switch
        {
            DatabaseProvider.PostgreSql => new NpgsqlConnection(_connectionString),
            DatabaseProvider.SqlServer => new SqlConnection(_connectionString),
            _ => throw new NotSupportedException($"Proveedor de base de datos no soportado: {Provider}")
        };

        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw new DatabaseUnavailableException($"No se pudo conectar a la base de datos ({Provider}).", ex);
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
