using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;

namespace ApiPagos.Api.Extensions;

/// <summary>
/// Respuestas de error uniformes en español, incluidas las que genera el propio framework
/// (404, 405, 415, JSON mal formado, parámetros con formato inválido).
/// </summary>
internal static class ProblemDetailsExtensions
{
    private const string BodyKey = "body";
    private const string InvalidFormat = "Valor o formato inválido.";
    private const string InvalidBody = "El cuerpo de la solicitud es obligatorio y debe ser un JSON válido.";

    private static readonly Dictionary<int, (string Title, string? Detail)> SpanishDefaults = new()
    {
        [StatusCodes.Status400BadRequest] = ("Solicitud inválida.", null),
        [StatusCodes.Status401Unauthorized] = ("No autorizado.", "Envíe una API Key válida en el header X-Api-Key o un token Bearer válido."),
        [StatusCodes.Status403Forbidden] = ("Acceso denegado.", null),
        [StatusCodes.Status404NotFound] = ("Recurso no encontrado.", "La ruta solicitada no existe."),
        [StatusCodes.Status405MethodNotAllowed] = ("Método no permitido.", "El método HTTP no está permitido para esta ruta."),
        [StatusCodes.Status406NotAcceptable] = ("Formato de respuesta no soportado.", "Solo se responde application/json."),
        [StatusCodes.Status413PayloadTooLarge] = ("Contenido demasiado grande.", null),
        [StatusCodes.Status415UnsupportedMediaType] = ("Tipo de contenido no soportado.", "Envíe el cuerpo con Content-Type: application/json."),
        [StatusCodes.Status429TooManyRequests] = ("Demasiadas solicitudes.", "Se superó el límite de solicitudes. Intente nuevamente más tarde."),
        [StatusCodes.Status500InternalServerError] = ("Ocurrió un error inesperado.", null),
        [StatusCodes.Status503ServiceUnavailable] = ("Servicio temporalmente no disponible.", null)
    };

    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                var problem = context.ProblemDetails;
                var status = problem.Status ?? context.HttpContext.Response.StatusCode;

                if (SpanishDefaults.TryGetValue(status, out var spanish) && IsFrameworkDefaultTitle(problem.Title, status))
                {
                    problem.Title = spanish.Title;
                    problem.Detail ??= spanish.Detail;
                }

                problem.Instance = context.HttpContext.Request.Path;
                problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            });

        // PostConfigure: MVC define su propia fábrica al registrar los controllers y la sobrescribiría.
        services.PostConfigure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = context =>
            {
                var problem = new ValidationProblemDetails(TranslateModelState(context.ModelState))
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    Status = StatusCodes.Status400BadRequest,
                    Title = "La solicitud contiene datos inválidos.",
                    Instance = context.HttpContext.Request.Path
                };
                problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                return new BadRequestObjectResult(problem)
                {
                    ContentTypes = { "application/problem+json" }
                };
            });

        return services;
    }

    private static bool IsFrameworkDefaultTitle(string? title, int status) =>
        title is null
        || title == ReasonPhrases.GetReasonPhrase(status)
        || title == "One or more validation errors occurred."
        || title == "An error occurred while processing your request.";

    private static Dictionary<string, string[]> TranslateModelState(ModelStateDictionary modelState)
    {
        var errors = new Dictionary<string, List<string>>();
        var hasJsonPathErrors = modelState.Keys.Any(k => k.StartsWith('$'));

        foreach (var (key, entry) in modelState)
        {
            if (entry.Errors.Count == 0)
            {
                continue;
            }

            var isBodyKey = key.Length == 0 || key == "$" || IsActionArgument(key);

            // Si ya hay un error de un campo concreto del JSON, el genérico "request is required" sobra.
            if (isBodyKey && key != "$" && hasJsonPathErrors)
            {
                continue;
            }

            var field = isBodyKey ? BodyKey : NormalizeKey(key);
            var messages = errors.TryGetValue(field, out var list) ? list : errors[field] = [];

            foreach (var error in entry.Errors)
            {
                var message = isBodyKey ? InvalidBody : Translate(error);
                if (!messages.Contains(message))
                {
                    messages.Add(message);
                }
            }
        }

        return errors.ToDictionary(e => e.Key, e => e.Value.ToArray());
    }

    // Claves que ASP.NET usa para el parámetro del action completo (ej. "request").
    private static bool IsActionArgument(string key) => key is "request";

    private static string NormalizeKey(string key)
    {
        var trimmed = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key.TrimStart('$');
        return JsonNamingPolicy.CamelCase.ConvertName(trimmed);
    }

    private static string Translate(ModelError error)
    {
        var message = error.ErrorMessage;

        if (error.Exception is not null
            || string.IsNullOrEmpty(message)
            || message.Contains("is not valid", StringComparison.OrdinalIgnoreCase)
            || message.Contains("could not be converted", StringComparison.OrdinalIgnoreCase)
            || message.Contains("was not valid", StringComparison.OrdinalIgnoreCase))
        {
            return InvalidFormat;
        }

        return message;
    }
}
