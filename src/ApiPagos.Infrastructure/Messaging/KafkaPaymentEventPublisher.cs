using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using ApiPagos.Application.Abstractions;
using ApiPagos.Application.Payments.Events;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiPagos.Infrastructure.Messaging;

internal sealed class KafkaPaymentEventPublisher(
    IProducer<string, string> producer,
    IOptions<KafkaOptions> options,
    ILogger<KafkaPaymentEventPublisher> logger) : IPaymentEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public async Task PublishAsync(PaymentRegisteredEvent paymentEvent, CancellationToken cancellationToken)
    {
        var message = new Message<string, string>
        {
            // La clave por cliente mantiene el orden de sus eventos dentro de la partición.
            Key = paymentEvent.CustomerId.ToString(),
            Value = JsonSerializer.Serialize(paymentEvent, JsonOptions),
            Headers = new Headers { { "event-type", Encoding.UTF8.GetBytes(paymentEvent.EventType) } }
        };

        var result = await producer.ProduceAsync(options.Value.Topic, message, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Evento {EventType} del pago {PaymentId} publicado en {TopicPartitionOffset}",
            paymentEvent.EventType, paymentEvent.PaymentId, result.TopicPartitionOffset);
    }
}

internal sealed class NullPaymentEventPublisher : IPaymentEventPublisher
{
    public Task PublishAsync(PaymentRegisteredEvent paymentEvent, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
