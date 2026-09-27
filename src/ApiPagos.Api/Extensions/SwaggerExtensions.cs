using ApiPagos.Api.Security;
using Microsoft.OpenApi.Models;

namespace ApiPagos.Api.Extensions;

internal static class SwaggerExtensions
{
    public const string BearerScheme = "Bearer";

    public static IServiceCollection AddApiSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "API de Pagos de Servicios Básicos",
                Version = "v1",
                Description = "Registro y consulta de pagos de servicios básicos (agua, electricidad, telecomunicaciones)."
            });

            options.AddSecurityDefinition(AuthSchemes.ApiKey, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = AuthSchemes.ApiKeyHeader,
                Description = "API Key del cliente."
            });

            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Token obtenido en POST /api/auth/token."
            });

            options.SchemaFilter<SwaggerExamplesFilter>();
            options.OperationFilter<SwaggerOperationFilter>();

            foreach (var xml in Directory.GetFiles(AppContext.BaseDirectory, "ApiPagos.*.xml"))
            {
                options.IncludeXmlComments(xml);
            }
        });

        return services;
    }

    public static OpenApiSecurityRequirement Requirement(string schemeId) => new()
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = schemeId }
        }] = []
    };
}
