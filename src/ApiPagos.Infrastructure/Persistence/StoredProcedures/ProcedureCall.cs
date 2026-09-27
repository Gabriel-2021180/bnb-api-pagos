using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace ApiPagos.Infrastructure.Persistence.StoredProcedures;

/// <summary>
/// Traduce la invocación de un procedimiento almacenado al formato de cada motor.
/// SQL Server usa CommandType.StoredProcedure; en PostgreSQL los procedimientos que devuelven
/// filas son funciones y se invocan con notación nombrada (p_parametro => @parametro).
/// </summary>
internal static partial class ProcedureCall
{
    public const string PostgreSqlParameterPrefix = "p_";

    public static (string CommandText, CommandType CommandType) Build(
        DatabaseProvider provider,
        string procedureName,
        IEnumerable<string> parameterNames)
    {
        if (!IdentifierRegex().IsMatch(procedureName))
        {
            throw new ArgumentException($"Nombre de procedimiento inválido: {procedureName}", nameof(procedureName));
        }

        return provider switch
        {
            DatabaseProvider.SqlServer => (procedureName, CommandType.StoredProcedure),
            DatabaseProvider.PostgreSql => (BuildPostgreSqlCall(procedureName, parameterNames), CommandType.Text),
            _ => throw new NotSupportedException($"Proveedor de base de datos no soportado: {provider}")
        };
    }

    private static string BuildPostgreSqlCall(string procedureName, IEnumerable<string> parameterNames)
    {
        var sb = new StringBuilder("SELECT * FROM ").Append(procedureName).Append('(');
        var first = true;

        foreach (var name in parameterNames)
        {
            if (!IdentifierRegex().IsMatch(name))
            {
                throw new ArgumentException($"Nombre de parámetro inválido: {name}", nameof(parameterNames));
            }

            if (!first)
            {
                sb.Append(", ");
            }

            sb.Append(PostgreSqlParameterPrefix).Append(name).Append(" => @").Append(name);
            first = false;
        }

        return sb.Append(')').ToString();
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)?$")]
    private static partial Regex IdentifierRegex();
}
