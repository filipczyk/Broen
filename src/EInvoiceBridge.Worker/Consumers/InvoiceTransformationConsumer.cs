using EInvoiceBridge.Application.Helpers;
using EInvoiceBridge.Core.Enums;
using EInvoiceBridge.Core.Events;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Infrastructure.Kafka;
using Microsoft.Extensions.DependencyInjection;
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
        using var scope = _serviceProvider.CreateScope();
        var invoiceRepo = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditRepository>();
        var formatRepo = scope.ServiceProvider.GetRequiredService<IFormatRepository>();
        var transformationService = scope.ServiceProvider.GetRequiredService<ITransformationService>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var invoice = await invoiceRepo.GetByIdAsync(@event.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            Logger.LogWarning("Invoice {InvoiceId} not found", @event.InvoiceId);
            return;
        }

        try
        {
            await invoiceRepo.UpdateStatusAsync(invoice.Id, InvoiceStatus.Transforming.ToString(), cancellationToken: cancellationToken);
            await auditRepo.InsertAuditEntryAsync(invoice.Id, InvoiceStatus.Transforming.ToString(), "Transformation started", null, cancellationToken);

            var hydrated = InvoiceReconstructor.Hydrate(invoice);

            var formatVersion = await formatRepo.GetActiveFormatAsync(hydrated.Buyer.Address.CountryCode, cancellationToken);
            if (formatVersion is null)
            {
                Logger.LogError("No active format found for country {Country} for invoice {InvoiceId}",
                    hydrated.Buyer.Address.CountryCode, @event.InvoiceId);
                await invoiceRepo.UpdateStatusAsync(invoice.Id, InvoiceStatus.Failed.ToString(), cancellationToken: cancellationToken);
                await auditRepo.InsertAuditEntryAsync(invoice.Id, InvoiceStatus.Failed.ToString(),
                    $"No active format for country {hydrated.Buyer.Address.CountryCode}", null, cancellationToken);
                await eventPublisher.PublishAsync(new InvoiceFailed(@event.InvoiceId, "No active format version"), cancellationToken);
                return;
            }

            var xml = await transformationService.TransformToUblXmlAsync(hydrated, formatVersion, cancellationToken);
            await invoiceRepo.UpdateStatusAsync(invoice.Id, InvoiceStatus.Transforming.ToString(),
                generatedXml: xml, cancellationToken: cancellationToken);
            await auditRepo.InsertAuditEntryAsync(invoice.Id, InvoiceStatus.Transforming.ToString(), "UBL XML generated", null, cancellationToken);
            await eventPublisher.PublishAsync(new InvoiceTransformed(@event.InvoiceId), cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error transforming invoice {InvoiceId}", @event.InvoiceId);
            await invoiceRepo.UpdateStatusAsync(invoice.Id, InvoiceStatus.Failed.ToString(), cancellationToken: cancellationToken);
            await auditRepo.InsertAuditEntryAsync(invoice.Id, InvoiceStatus.Failed.ToString(), $"Transformation error: {ex.Message}", null, cancellationToken);
            await eventPublisher.PublishAsync(new InvoiceFailed(@event.InvoiceId, ex.Message), cancellationToken);
        }
    }
}
