using System.Data;
using Dapper;

namespace ApiPagos.Infrastructure.Persistence.StoredProcedures;

/// <summary>
/// Único punto de acceso a la base de datos: solo ejecuta procedimientos almacenados.
/// </summary>
internal interface IStoredProcedureExecutor
{
    Task<IReadOnlyList<T>> QueryAsync<T>(
        string procedureName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken);

    Task<T> QuerySingleAsync<T>(
        string procedureName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken);
}

internal sealed class StoredProcedureExecutor(IDbConnectionFactory connectionFactory) : IStoredProcedureExecutor
{
    public async Task<IReadOnlyList<T>> QueryAsync<T>(
        string procedureName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = BuildCommand(procedureName, parameters, cancellationToken);
        var rows = await connection.QueryAsync<T>(command).ConfigureAwait(false);
        return rows.AsList();
    }

    public async Task<T> QuerySingleAsync<T>(
        string procedureName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = BuildCommand(procedureName, parameters, cancellationToken);
        return await connection.QuerySingleAsync<T>(command).ConfigureAwait(false);
    }

    private CommandDefinition BuildCommand(
        string procedureName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var (commandText, commandType) = ProcedureCall.Build(connectionFactory.Provider, procedureName, parameters.Keys);

        var dynamicParameters = new DynamicParameters();
        foreach (var (name, value) in parameters)
        {
            // Por defecto Dapper envía DateTime como "datetime" en SQL Server y se pierde precisión.
            DbType? dbType = value is DateTime && connectionFactory.Provider == DatabaseProvider.SqlServer
                ? DbType.DateTime2
                : null;
            dynamicParameters.Add(name, value, dbType);
        }

        return new CommandDefinition(
            commandText,
            dynamicParameters,
            commandType: commandType,
            cancellationToken: cancellationToken);
    }
}
