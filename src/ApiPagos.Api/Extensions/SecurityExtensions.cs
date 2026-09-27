using System.Security.Claims;
using System.Threading.RateLimiting;
using ApiPagos.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ApiPagos.Api.Extensions;

internal static class SecurityExtensions
{
    public const string CorsPolicy = "default";
    public const string AuthRateLimitPolicy = "auth";

    public static IServiceCollection AddApiSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SecurityOptions>()
            .Bind(configuration.GetSection(SecurityOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SecurityOptions>, SecurityOptionsValidator>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddAuthentication(AuthSchemes.ApiKeyOrJwt)
            .AddPolicyScheme(AuthSchemes.ApiKeyOrJwt, "API Key o JWT", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    string? authorization = context.Request.Headers.Authorization;
                    return authorization?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
                        ? JwtBearerDefaults.AuthenticationScheme
                        : AuthSchemes.ApiKey;
                };
            })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(AuthSchemes.ApiKey, null)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<SecurityOptions>>((options, security) =>
            {
                var jwt = security.Value.Jwt;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = JwtTokenService.CreateSigningKey(jwt.SigningKey),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Sub
                };
            });

        // Todo endpoint exige autenticación salvo los marcados con [AllowAnonymous].
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder(AuthSchemes.ApiKeyOrJwt)
                .RequireAuthenticatedUser()
                .Build());

        var origins = configuration.GetSection($"{SecurityOptions.SectionName}:CorsAllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
        {
            if (origins.Length > 0)
            {
                policy.WithOrigins(origins).WithMethods("GET", "POST")
                    .WithHeaders("Content-Type", "Authorization", AuthSchemes.ApiKeyHeader);
            }
        }));

        services.AddApiRateLimiting(configuration);

        return services;
    }

    private static void AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("RateLimiting");
        var permitLimit = section.GetValue("PermitLimit", 100);
        var window = TimeSpan.FromSeconds(section.GetValue("WindowSeconds", 60));
        var authPermitLimit = section.GetValue("AuthPermitLimit", 10);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    PartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window }));

            options.AddPolicy(AuthRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = authPermitLimit, Window = window }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                var problemDetails = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Demasiadas solicitudes.",
                        Detail = "Se superó el límite de solicitudes. Intente nuevamente más tarde."
                    }
                }).ConfigureAwait(false);
            };
        });
    }

    private static string PartitionKey(HttpContext context) =>
        context.User.FindFirstValue(ClaimTypes.Name)
        ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";
}
