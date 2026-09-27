using System.Security.Cryptography;
using System.Text;

namespace ApiPagos.Api.Security;

internal static class SecretComparer
{
    /// <summary>
    /// Comparación en tiempo constante; se comparan los hashes para no filtrar la longitud del secreto.
    /// </summary>
    public static bool AreEqual(string? provided, string expected)
    {
        if (string.IsNullOrEmpty(provided) || string.IsNullOrEmpty(expected))
        {
            return false;
        }

        var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(provided));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));

        return CryptographicOperations.FixedTimeEquals(providedHash, expectedHash);
    }
}
