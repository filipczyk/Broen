using System.Text.Json;
using Confluent.Kafka;
using EInvoiceBridge.Core.Events;
using EInvoiceBridge.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EInvoiceBridge.Infrastructure.Kafka;

public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(IOptions<KafkaOptions> options, ILogger<KafkaEventPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    internal KafkaEventPublisher(IProducer<string, string> producer, KafkaOptions options, ILogger<KafkaEventPublisher> logger)
    {
        _producer = producer;
        _options = options;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IIntegrationEvent
    {
        var topic = $"{_options.TopicPrefix}.{@event.EventType}";

        var json = JsonSerializer.Serialize(@event, @event.GetType());

        var key = ExtractMessageKey(json, @event.EventId);

        await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = key,
            Value = json
        }, cancellationToken);

        _logger.LogInformation("Published {EventType} to {Topic} with key {Key}", @event.EventType, topic, key);
    }

    private static string ExtractMessageKey(string json, Guid fallbackEventId)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("InvoiceId", out var invoiceIdProp))
            {
                return invoiceIdProp.GetGuid().ToString();
            }
        }
        catch
        {
            // Fall through to fallback
        }

        return fallbackEventId.ToString();
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}
