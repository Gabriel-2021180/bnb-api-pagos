namespace ApiPagos.Api.Security;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public IList<ApiKeyClient> ApiKeys { get; } = new List<ApiKeyClient>();

    public JwtOptions Jwt { get; set; } = new();

    public IList<string> CorsAllowedOrigins { get; } = new List<string>();

    /// <summary>Activar solo si la API atiende HTTPS directamente (no detrás de un proxy que termina TLS).</summary>
    public bool UseHttpsRedirection { get; set; }
}

public sealed class ApiKeyClient
{
    public string Name { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;
}

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "apipagos";

    public string Audience { get; set; } = "apipagos-clients";

    /// <summary>Clave HMAC-SHA256; mínimo 32 caracteres.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 60;

    public IList<JwtClient> Clients { get; } = new List<JwtClient>();
}

public sealed class JwtClient
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;
}
