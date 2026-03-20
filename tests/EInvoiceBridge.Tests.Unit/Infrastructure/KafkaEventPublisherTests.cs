using System.Text.Json;
using Confluent.Kafka;
using EInvoiceBridge.Core.Events;
using EInvoiceBridge.Infrastructure.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace EInvoiceBridge.Tests.Unit.Infrastructure;

public class KafkaEventPublisherTests
{
    private readonly IProducer<string, string> _producer = Substitute.For<IProducer<string, string>>();
    private readonly ILogger<KafkaEventPublisher> _logger = Substitute.For<ILogger<KafkaEventPublisher>>();
    private readonly KafkaOptions _options = new() { TopicPrefix = "einvoice" };

    private KafkaEventPublisher CreatePublisher() => new(_producer, _options, _logger);

    [Fact]
    public async Task PublishAsync_UsesCorrectTopic()
    {
        var publisher = CreatePublisher();
        var invoiceId = Guid.NewGuid();
        var @event = new InvoiceReceived(invoiceId, "INV-001");

        _producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new DeliveryResult<string, string>());

        await publisher.PublishAsync(@event);

        await _producer.Received(1).ProduceAsync(
            "einvoice.invoice.received",
            Arg.Any<Message<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_UsesInvoiceIdAsMessageKey()
    {
        var publisher = CreatePublisher();
        var invoiceId = Guid.NewGuid();
        var @event = new InvoiceReceived(invoiceId, "INV-001");

        _producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new DeliveryResult<string, string>());

        await publisher.PublishAsync(@event);

        await _producer.Received(1).ProduceAsync(
            Arg.Any<string>(),
            Arg.Is<Message<string, string>>(m => m.Key == invoiceId.ToString()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_SerializesAllEventProperties()
    {
        var publisher = CreatePublisher();
        var invoiceId = Guid.NewGuid();
        var @event = new InvoiceReceived(invoiceId, "INV-001");
        Message<string, string>? capturedMessage = null;

        _producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedMessage = callInfo.ArgAt<Message<string, string>>(1);
                return new DeliveryResult<string, string>();
            });

        await publisher.PublishAsync(@event);

        capturedMessage.Should().NotBeNull();
        var json = capturedMessage!.Value;
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("InvoiceId").GetGuid().Should().Be(invoiceId);
        doc.RootElement.GetProperty("InvoiceNumber").GetString().Should().Be("INV-001");
        doc.RootElement.GetProperty("EventType").GetString().Should().Be("invoice.received");
    }

    [Fact]
    public async Task PublishAsync_ForEventWithoutInvoiceId_FallsBackToEventId()
    {
        // InvoiceDelivered has InvoiceId, so it should use that
        var publisher = CreatePublisher();
        var invoiceId = Guid.NewGuid();
        var @event = new InvoiceDelivered(invoiceId);

        _producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new DeliveryResult<string, string>());

        await publisher.PublishAsync(@event);

        await _producer.Received(1).ProduceAsync(
            "einvoice.invoice.delivered",
            Arg.Is<Message<string, string>>(m => m.Key == invoiceId.ToString()),
            Arg.Any<CancellationToken>());
    }
}
