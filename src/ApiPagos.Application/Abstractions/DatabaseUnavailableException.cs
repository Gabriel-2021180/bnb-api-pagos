namespace ApiPagos.Application.Abstractions;

/// <summary>
/// No se pudo establecer conexión con la base de datos (servidor caído, DNS, red, timeout).
/// </summary>
public sealed class DatabaseUnavailableException : Exception
{
    public DatabaseUnavailableException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public DatabaseUnavailableException(string message) : base(message)
    {
    }

    public DatabaseUnavailableException()
    {
    }
}
