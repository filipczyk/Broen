using EInvoiceBridge.Core.Events;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Infrastructure.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EInvoiceBridge.Worker.Consumers;

public sealed class InvoiceDeliveryConsumer : KafkaConsumerBase<InvoiceTransformed>
{
    private readonly IServiceProvider _serviceProvider;

    protected override string Topic => "einvoice.invoice.transformed";

    public InvoiceDeliveryConsumer(
        IOptions<KafkaOptions> options,
        ILogger<InvoiceDeliveryConsumer> logger,
        IServiceProvider serviceProvider)
        : base(options, logger)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleAsync(InvoiceTransformed @event, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
