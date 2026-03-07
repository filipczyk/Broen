using EInvoiceBridge.Core.Events;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Infrastructure.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EInvoiceBridge.Worker.Consumers;

public sealed class InvoiceTransformationConsumer : KafkaConsumerBase<InvoiceValidated>
{
    private readonly IServiceProvider _serviceProvider;

    protected override string Topic => "einvoice.invoice.validated";

    public InvoiceTransformationConsumer(
        IOptions<KafkaOptions> options,
        ILogger<InvoiceTransformationConsumer> logger,
        IServiceProvider serviceProvider)
        : base(options, logger)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleAsync(InvoiceValidated @event, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
