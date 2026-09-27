namespace ApiPagos.Domain.Common;

/// <summary>
/// Violación de una regla de negocio del dominio.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException()
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
