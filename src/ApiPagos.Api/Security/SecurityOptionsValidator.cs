using Microsoft.Extensions.Options;

namespace ApiPagos.Api.Security;

internal sealed class SecurityOptionsValidator : IValidateOptions<SecurityOptions>
{
    public const int MinSigningKeyLength = 32;
    public const int MinSecretLength = 16;

    public ValidateOptionsResult Validate(string? name, SecurityOptions options)
    {
        var errors = new List<string>();

        if (options.Jwt.SigningKey.Length < MinSigningKeyLength)
        {
            errors.Add($"Security:Jwt:SigningKey debe tener al menos {MinSigningKeyLength} caracteres.");
        }

        if (string.IsNullOrWhiteSpace(options.Jwt.Issuer) || string.IsNullOrWhiteSpace(options.Jwt.Audience))
        {
            errors.Add("Security:Jwt:Issuer y Security:Jwt:Audience son obligatorios.");
        }

        if (options.Jwt.ExpirationMinutes is < 1 or > 1440)
        {
            errors.Add("Security:Jwt:ExpirationMinutes debe estar entre 1 y 1440.");
        }

        if (options.ApiKeys.Any(k => string.IsNullOrWhiteSpace(k.Name) || k.Key.Length < MinSecretLength))
        {
            errors.Add($"Cada Security:ApiKeys debe tener Name y un Key de al menos {MinSecretLength} caracteres.");
        }

        if (options.Jwt.Clients.Any(c => string.IsNullOrWhiteSpace(c.ClientId) || c.ClientSecret.Length < MinSecretLength))
        {
            errors.Add($"Cada Security:Jwt:Clients debe tener ClientId y un ClientSecret de al menos {MinSecretLength} caracteres.");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
