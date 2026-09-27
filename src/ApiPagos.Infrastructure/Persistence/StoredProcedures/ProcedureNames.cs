namespace ApiPagos.Infrastructure.Persistence.StoredProcedures;

/// <summary>
/// Nombres de los procedimientos definidos en /database (mismos nombres en PostgreSQL y SQL Server).
/// </summary>
internal static class ProcedureNames
{
    public const string PaymentCreate = "payments.sp_payment_create";
    public const string PaymentGetByCustomer = "payments.sp_payment_get_by_customer";
    public const string HealthCheck = "payments.sp_health_check";
}
