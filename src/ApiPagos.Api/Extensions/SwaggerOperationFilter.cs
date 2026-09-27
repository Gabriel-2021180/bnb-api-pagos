using ApiPagos.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiPagos.Api.Extensions;

/// <summary>
/// Descripciones de respuesta en español y requisito de seguridad solo en los endpoints protegidos.
/// </summary>
internal sealed class SwaggerOperationFilter : IOperationFilter
{
    private static readonly Dictionary<string, string> Descriptions = new()
    {
        ["400"] = "Datos inválidos.",
        ["401"] = "Falta la API Key / token o no es válido.",
        ["413"] = "El cuerpo supera el tamaño máximo.",
        ["415"] = "Content-Type no soportado (use application/json).",
        ["429"] = "Se superó el límite de solicitudes.",
        ["500"] = "Error inesperado (ver traceId en los logs).",
        ["503"] = "Base de datos no disponible temporalmente."
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var (code, response) in operation.Responses)
        {
            // Solo se reemplazan las descripciones genéricas en inglés que genera Swashbuckle.
            if (Descriptions.TryGetValue(code, out var spanish) && IsDefaultDescription(response.Description))
            {
                response.Description = spanish;
            }
        }

        // Dos requisitos separados = cualquiera de los dos (API Key o Bearer) es suficiente.
        var isAnonymous = context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();
        if (!isAnonymous)
        {
            operation.Security =
            [
                SwaggerExtensions.Requirement(AuthSchemes.ApiKey),
                SwaggerExtensions.Requirement(SwaggerExtensions.BearerScheme)
            ];
        }
    }

    private static bool IsDefaultDescription(string? description) =>
        description is null or "" or "Bad Request" or "Unauthorized" or "Payload Too Large"
            or "Unsupported Media Type" or "Too Many Requests" or "Internal Server Error" or "Service Unavailable";
}
