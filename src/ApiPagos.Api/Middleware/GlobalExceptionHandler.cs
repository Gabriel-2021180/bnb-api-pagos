using System.Text.Json;
using ApiPagos.Application.Abstractions;
using ApiPagos.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ApiPagos.Api.Middleware;

/// <summary>
/// Convierte las excepciones en respuestas ProblemDetails (RFC 7807) sin exponer detalles internos.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // El cliente cerró la conexión: no hay a quién responder.
            return true;
        }

        var problem = Map(exception);

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Error en {Method} {Path}: {Title}",
                httpContext.Request.Method, httpContext.Request.Path, problem.Title);
        }
        else
        {
            logger.LogInformation("Solicitud rechazada ({Status}): {Message}", problem.Status, exception.Message);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        }).ConfigureAwait(false);
    }

    private static ProblemDetails Map(Exception exception) => exception switch
    {
        ValidationException validation => new ValidationProblemDetails(
            validation.Errors
                .GroupBy(e => JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName))
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "La solicitud contiene datos inválidos."
        },

        DomainException domain => new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Regla de negocio incumplida.",
            Detail = domain.Message
        },

        // Errores de Kestrel al leer la solicitud, por ejemplo body mayor al límite (413).
        BadHttpRequestException badRequest => new ProblemDetails
        {
            Type = badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge
                ? "https://tools.ietf.org/html/rfc9110#section-15.5.14"
                : "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Status = badRequest.StatusCode,
            Title = badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge
                ? "Contenido demasiado grande."
                : "Solicitud inválida.",
            Detail = badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge
                ? "El cuerpo de la solicitud supera el tamaño máximo permitido."
                : "No se pudo leer la solicitud."
        },

        DatabaseUnavailableException or TimeoutException => new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "Servicio temporalmente no disponible.",
            Detail = "No se pudo acceder a la base de datos. Intente nuevamente en unos momentos."
        },

        _ => new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Ocurrió un error inesperado.",
            Detail = "Intente nuevamente. Si el problema persiste, contacte a soporte con el traceId."
        }
    };
}
