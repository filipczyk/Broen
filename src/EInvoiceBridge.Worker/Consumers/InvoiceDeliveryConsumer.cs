using EInvoiceBridge.Application.Helpers;
using EInvoiceBridge.Core.Enums;
using EInvoiceBridge.Core.Events;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Infrastructure.Kafka;
using Microsoft.Extensions.DependencyInjection;
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
        using var scope = _serviceProvider.CreateScope();
        var invoiceRepo = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditRepository>();
        var deliveryService = scope.ServiceProvider.GetRequiredService<IDeliveryService>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var invoice = await invoiceRepo.GetByIdAsync(@event.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            Logger.LogWarning("Invoice {InvoiceId} not found", @event.InvoiceId);
            return;
        }

        if (string.IsNullOrEmpty(invoice.GeneratedXml))
        {
            Logger.LogError("Invoice {InvoiceId} has no generated XML", @event.InvoiceId);
            await invoiceRepo.UpdateStatusAsync(invoice.Id, InvoiceStatus.Failed.ToString(), cancellationToken: cancellationToken);
            await auditRepo.InsertAuditEntryAsync(invoice.Id, InvoiceStatus.Failed.ToString(), "No generated XML available", null, cancellationToken);
            await eventPublisher.PublishAsync(new InvoiceFailed(@event.InvoiceId, "No generated XML"), cancellationToken);
            return;
        }

        try
        {
            await invoiceRepo.UpdateStatusAsync(invoice.Id, InvoiceStatus.Sending.ToString(), cancellationToken: cancellationToken);
            await auditRepo.InsertAuditEntryAsync(invoice.Id, InvoiceStatus.Sending.ToString(), "Delivery started", null, cancellationToken);

            var hydrated = InvoiceReconstructor.Hydrate(invoice);
            var submissionId = await deliveryService.SubmitAsync(invoice.Id, invoice.GeneratedXml, hydrated.Buyer.VatNumber, cancellationToken);

            await invoiceRepo.UpdateStatusAsync(invoice.Id, InvoiceStatus.Sent.ToString(),
                storecoveSubmissionId: submissionId, cancellationToken: cancellationToken);
            await auditRepo.InsertAuditEntryAsync(invoice.Id, InvoiceStatus.Sent.ToString(),
                $"Submitted to Storecove (submission: {submissionId})", null, cancellationToken);
            await eventPublisher.PublishAsync(new InvoiceSent(@event.InvoiceId, submissionId), cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error delivering invoice {InvoiceId}", @event.InvoiceId);
            await invoiceRepo.UpdateStatusAsync(invoice.Id, InvoiceStatus.Failed.ToString(), cancellationToken: cancellationToken);
            await auditRepo.InsertAuditEntryAsync(invoice.Id, InvoiceStatus.Failed.ToString(), $"Delivery error: {ex.Message}", null, cancellationToken);
            await eventPublisher.PublishAsync(new InvoiceFailed(@event.InvoiceId, ex.Message), cancellationToken);
        }
    }
}
