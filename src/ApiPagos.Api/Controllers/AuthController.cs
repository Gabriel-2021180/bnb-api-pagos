using System.ComponentModel.DataAnnotations;
using ApiPagos.Api.Extensions;
using ApiPagos.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ApiPagos.Api.Controllers;

/// <summary>Credenciales de un cliente configurado en Security:Jwt:Clients.</summary>
public sealed record TokenRequest(
    [Required(ErrorMessage = "El clientId es obligatorio.")] string ClientId,
    [Required(ErrorMessage = "El clientSecret es obligatorio.")] string ClientSecret);

public sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class AuthController(IJwtTokenService tokenService) : ControllerBase
{
    /// <summary>
    /// Emite un JWT Bearer para un cliente registrado.
    /// </summary>
    /// <response code="200">Token emitido.</response>
    /// <response code="401">Credenciales inválidas.</response>
    [HttpPost("token")]
    [Consumes("application/json")]
    [EnableRateLimiting(SecurityExtensions.AuthRateLimitPolicy)]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public ActionResult<TokenResponse> Token([FromBody] TokenRequest request)
    {
        var token = tokenService.Issue(request.ClientId, request.ClientSecret);
        if (token is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "No autorizado.",
                detail: "Credenciales de cliente inválidas.");
        }

        return Ok(new TokenResponse(token.AccessToken, "Bearer", token.ExpiresIn));
    }
}
