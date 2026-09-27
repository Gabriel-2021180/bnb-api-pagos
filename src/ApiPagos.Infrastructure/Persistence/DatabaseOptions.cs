using System.ComponentModel.DataAnnotations;

namespace ApiPagos.Infrastructure.Persistence;

public enum DatabaseProvider
{
    PostgreSql,
    SqlServer
}

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required(ErrorMessage = "Database:Provider es obligatorio (PostgreSql o SqlServer).")]
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.PostgreSql;

    [Required(ErrorMessage = "Database:Host es obligatorio.")]
    public string Host { get; set; } = string.Empty;

    /// <summary>Si no se indica se usa el puerto por defecto del proveedor (5432 / 1433).</summary>
    [Range(1, 65535, ErrorMessage = "Database:Port debe estar entre 1 y 65535.")]
    public int? Port { get; set; }

    [Required(ErrorMessage = "Database:Name es obligatorio.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Database:Username es obligatorio.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Database:Password es obligatorio.")]
    public string Password { get; set; } = string.Empty;

    public bool UseSsl { get; set; }

    public bool TrustServerCertificate { get; set; } = true;

    public bool Pooling { get; set; } = true;

    [Range(1, 1000, ErrorMessage = "Database:MaxPoolSize debe estar entre 1 y 1000.")]
    public int MaxPoolSize { get; set; } = 100;

    [Range(1, 300, ErrorMessage = "Database:ConnectTimeoutSeconds debe estar entre 1 y 300.")]
    public int ConnectTimeoutSeconds { get; set; } = 15;

    [Range(1, 600, ErrorMessage = "Database:CommandTimeoutSeconds debe estar entre 1 y 600.")]
    public int CommandTimeoutSeconds { get; set; } = 30;

    public string ApplicationName { get; set; } = "ApiPagos";
}
