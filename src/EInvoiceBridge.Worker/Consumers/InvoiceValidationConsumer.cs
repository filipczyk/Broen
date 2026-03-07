using EInvoiceBridge.Core.Events;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Infrastructure.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EInvoiceBridge.Worker.Consumers;

public sealed class InvoiceValidationConsumer : KafkaConsumerBase<InvoiceReceived>
{
    private readonly IServiceProvider _serviceProvider;

    protected override string Topic => "einvoice.invoice.received";

    public InvoiceValidationConsumer(
        IOptions<KafkaOptions> options,
        ILogger<InvoiceValidationConsumer> logger,
        IServiceProvider serviceProvider)
        : base(options, logger)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleAsync(InvoiceReceived @event, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
