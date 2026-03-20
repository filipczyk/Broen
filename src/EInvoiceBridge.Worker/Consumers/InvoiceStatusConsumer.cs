using EInvoiceBridge.Core.Enums;
using EInvoiceBridge.Core.Events;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Infrastructure.Kafka;
using Microsoft.Extensions.DependencyInjection;
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
        using var scope = _serviceProvider.CreateScope();
        var invoiceRepo = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditRepository>();

        var invoice = await invoiceRepo.GetByIdAsync(@event.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            Logger.LogWarning("Invoice {InvoiceId} not found", @event.InvoiceId);
            return;
        }

        await invoiceRepo.UpdateStatusAsync(invoice.Id, InvoiceStatus.Delivered.ToString(), cancellationToken: cancellationToken);
        await auditRepo.InsertAuditEntryAsync(invoice.Id, InvoiceStatus.Delivered.ToString(), "Invoice delivered", null, cancellationToken);
    }
}
