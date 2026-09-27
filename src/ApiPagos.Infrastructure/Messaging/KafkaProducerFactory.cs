using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace ApiPagos.Infrastructure.Messaging;

internal static class KafkaProducerFactory
{
    public static IProducer<string, string> Create(KafkaOptions options, ILogger logger)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers,
            ClientId = options.ClientId,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = options.MessageTimeoutMs
        };

        if (!string.IsNullOrWhiteSpace(options.SecurityProtocol))
        {
            config.SecurityProtocol = Enum.Parse<SecurityProtocol>(options.SecurityProtocol, ignoreCase: true);
        }

        if (!string.IsNullOrWhiteSpace(options.SaslMechanism))
        {
            config.SaslMechanism = Enum.Parse<SaslMechanism>(options.SaslMechanism, ignoreCase: true);
            config.SaslUsername = options.SaslUsername;
            config.SaslPassword = options.SaslPassword;
        }

        return new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) => logger.LogWarning("Kafka: {Reason} ({Code})", error.Reason, error.Code))
            .Build();
    }
}
