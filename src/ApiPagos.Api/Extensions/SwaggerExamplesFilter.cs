using ApiPagos.Api.Controllers;
using ApiPagos.Application.Payments.Dtos;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiPagos.Api.Extensions;

/// <summary>
/// Ejemplos precargados en Swagger UI ("Try it out") con datos válidos.
/// </summary>
internal sealed class SwaggerExamplesFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(CreatePaymentRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["customerId"] = new OpenApiString("cfe8b150-2f84-4a1a-bdf4-923b20e34973"),
                ["serviceProvider"] = new OpenApiString("SERVICIOS ELÉCTRICOS S.A."),
                ["amount"] = new OpenApiDouble(120.50),
                ["currency"] = new OpenApiString("BOB")
            };
        }
        else if (context.Type == typeof(TokenRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["clientId"] = new OpenApiString("default-client"),
                ["clientSecret"] = new OpenApiString("<JWT_CLIENT_SECRET del .env>")
            };
        }
        else if (context.Type == typeof(PaymentResponse))
        {
            schema.Example = new OpenApiObject
            {
                ["paymentId"] = new OpenApiString("a248ad43-1f44-4b32-b0a0-e1c725b9bb7d"),
                ["serviceProvider"] = new OpenApiString("SERVICIOS ELÉCTRICOS S.A."),
                ["amount"] = new OpenApiDouble(120.50),
                ["status"] = new OpenApiString("pendiente"),
                ["createdAt"] = new OpenApiString("2025-07-17T08:30:00Z")
            };
        }
    }
}
