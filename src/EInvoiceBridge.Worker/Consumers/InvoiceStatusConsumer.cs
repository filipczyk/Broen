using EInvoiceBridge.Core.Events;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Infrastructure.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EInvoiceBridge.Worker.Consumers;

public sealed class InvoiceStatusConsumer : KafkaConsumerBase<InvoiceSent>
{
    private readonly IServiceProvider _serviceProvider;

    protected override string Topic => "einvoice.invoice.sent";

    public InvoiceStatusConsumer(
        IOptions<KafkaOptions> options,
        ILogger<InvoiceStatusConsumer> logger,
        IServiceProvider serviceProvider)
        : base(options, logger)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleAsync(InvoiceSent @event, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
