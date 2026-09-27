using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ApiPagos.Api.Security;

public sealed record TokenResult(string AccessToken, int ExpiresIn);

public interface IJwtTokenService
{
    TokenResult? Issue(string clientId, string clientSecret);
}

internal sealed class JwtTokenService(IOptionsMonitor<SecurityOptions> options, TimeProvider timeProvider) : IJwtTokenService
{
    public TokenResult? Issue(string clientId, string clientSecret)
    {
        var jwt = options.CurrentValue.Jwt;

        var client = jwt.Clients.FirstOrDefault(c =>
            string.Equals(c.ClientId, clientId, StringComparison.Ordinal));

        // Se compara siempre para no revelar por tiempo si el clientId existe.
        var validSecret = SecretComparer.AreEqual(clientSecret, client?.ClientSecret ?? Guid.NewGuid().ToString());
        if (client is null || !validSecret)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var lifetime = TimeSpan.FromMinutes(jwt.ExpirationMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.Add(lifetime),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, client.ClientId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("auth_method", "Jwt")
            ]),
            SigningCredentials = new SigningCredentials(CreateSigningKey(jwt.SigningKey), SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new TokenResult(token, (int)lifetime.TotalSeconds);
    }

    public static SymmetricSecurityKey CreateSigningKey(string signingKey) =>
        new(Encoding.UTF8.GetBytes(signingKey));
}
