using ApiPagos.Infrastructure.Persistence;

namespace ApiPagos.UnitTests.Infrastructure;

public class ConnectionStringFactoryTests
{
    private static DatabaseOptions Options(DatabaseProvider provider, int? port = null) => new()
    {
        Provider = provider,
        Host = "db-server",
        Port = port,
        Name = "apipagos",
        Username = "apipagos_app",
        Password = "secreto"
    };

    [Fact]
    public void PostgreSql_UsesConfiguredHostAndDefaultPort()
    {
        var cs = ConnectionStringFactory.Build(Options(DatabaseProvider.PostgreSql));

        Assert.Contains("Host=db-server", cs, StringComparison.Ordinal);
        Assert.Contains("Port=5432", cs, StringComparison.Ordinal);
        Assert.Contains("Database=apipagos", cs, StringComparison.Ordinal);
    }

    [Fact]
    public void SqlServer_UsesConfiguredHostAndCustomPort()
    {
        var cs = ConnectionStringFactory.Build(Options(DatabaseProvider.SqlServer, 14330));

        Assert.Contains("Data Source=db-server,14330", cs, StringComparison.Ordinal);
        Assert.Contains("Initial Catalog=apipagos", cs, StringComparison.Ordinal);
    }
}
