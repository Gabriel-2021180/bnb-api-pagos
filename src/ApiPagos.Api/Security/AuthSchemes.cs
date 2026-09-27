namespace ApiPagos.Api.Security;

public static class AuthSchemes
{
    public const string ApiKey = "ApiKey";
    public const string ApiKeyHeader = "X-Api-Key";
    public const string ApiKeyOrJwt = "ApiKeyOrJwt";
}
