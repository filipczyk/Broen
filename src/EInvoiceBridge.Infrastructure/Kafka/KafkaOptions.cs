namespace EInvoiceBridge.Infrastructure.Kafka;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "einvoice-workers";
    public string TopicPrefix { get; set; } = "einvoice";
}
