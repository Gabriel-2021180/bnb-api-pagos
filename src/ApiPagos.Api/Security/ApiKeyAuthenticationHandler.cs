using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ApiPagos.Api.Security;

internal sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptionsMonitor<SecurityOptions> securityOptions,
    IProblemDetailsService problemDetailsService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(AuthSchemes.ApiKeyHeader, out var values))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedKey = values.ToString();
        var client = securityOptions.CurrentValue.ApiKeys
            .FirstOrDefault(k => SecretComparer.AreEqual(providedKey, k.Key));

        if (client is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("API Key inválida."));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, client.Name), new Claim("auth_method", AuthSchemes.ApiKey)],
            Scheme.Name);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = $"{AuthSchemes.ApiKey} header=\"{AuthSchemes.ApiKeyHeader}\", Bearer";

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = Context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "No autorizado.",
                Detail = $"Envíe una API Key válida en el header {AuthSchemes.ApiKeyHeader} o un token Bearer válido."
            }
        }).ConfigureAwait(false);
    }
}
