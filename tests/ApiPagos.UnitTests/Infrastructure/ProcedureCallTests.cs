using System.Data;
using ApiPagos.Infrastructure.Persistence;
using ApiPagos.Infrastructure.Persistence.StoredProcedures;

namespace ApiPagos.UnitTests.Infrastructure;

public class ProcedureCallTests
{
    [Fact]
    public void SqlServer_UsesStoredProcedureCommandType()
    {
        var (text, type) = ProcedureCall.Build(DatabaseProvider.SqlServer, "payments.sp_x", ["customer_id"]);

        Assert.Equal("payments.sp_x", text);
        Assert.Equal(CommandType.StoredProcedure, type);
    }

    [Fact]
    public void PostgreSql_UsesNamedNotation()
    {
        var (text, type) = ProcedureCall.Build(DatabaseProvider.PostgreSql, "payments.sp_x", ["customer_id", "amount"]);

        Assert.Equal("SELECT * FROM payments.sp_x(p_customer_id => @customer_id, p_amount => @amount)", text);
        Assert.Equal(CommandType.Text, type);
    }

    [Theory]
    [InlineData("payments.sp_x; DROP TABLE x")]
    [InlineData("sp x")]
    public void InvalidProcedureName_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => ProcedureCall.Build(DatabaseProvider.PostgreSql, name, []));
    }
}
