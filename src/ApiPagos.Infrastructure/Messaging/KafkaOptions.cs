using System.ComponentModel.DataAnnotations;

namespace ApiPagos.Infrastructure.Messaging;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public bool Enabled { get; set; }

    public string BootstrapServers { get; set; } = "localhost:9094";

    [Required(ErrorMessage = "Kafka:Topic es obligatorio.")]
    public string Topic { get; set; } = "payments.registered";

    public string ClientId { get; set; } = "apipagos";

    [Range(1000, 300000, ErrorMessage = "Kafka:MessageTimeoutMs debe estar entre 1000 y 300000.")]
    public int MessageTimeoutMs { get; set; } = 10000;

    /// <summary>Opcional: Plaintext, Ssl, SaslPlaintext o SaslSsl.</summary>
    public string? SecurityProtocol { get; set; }

    /// <summary>Opcional: Plain, ScramSha256 o ScramSha512.</summary>
    public string? SaslMechanism { get; set; }

    public string? SaslUsername { get; set; }

    public string? SaslPassword { get; set; }
}
