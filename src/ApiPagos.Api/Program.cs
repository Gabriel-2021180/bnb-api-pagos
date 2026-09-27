using ApiPagos.Api.Extensions;
using ApiPagos.Api.Middleware;
using ApiPagos.Api.Security;
using ApiPagos.Application;
using ApiPagos.Infrastructure;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = builder.Configuration.GetValue<long>("RequestLimits:MaxBodySizeBytes", 32 * 1024);
});

builder.Services.AddApiProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.AllowInputFormatterExceptionMessages = false);
builder.Services.AddApiSwagger();
builder.Services.AddApiSecurity(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (!app.TryValidateConfiguration())
{
    return 1;
}

var security = app.Services.GetRequiredService<IOptions<SecurityOptions>>().Value;

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (security.UseHttpsRedirection)
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

if (app.Configuration.GetValue("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API de Pagos v1");
        options.DocumentTitle = "API de Pagos";
    });
}

app.UseCors(SecurityExtensions.CorsPolicy);
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapApiHealthChecks();

await app.RunAsync();
return 0;
