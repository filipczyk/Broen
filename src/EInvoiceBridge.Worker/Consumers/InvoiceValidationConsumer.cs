using System.Text.Json;
using EInvoiceBridge.Application.Helpers;
using EInvoiceBridge.Core.Enums;
using EInvoiceBridge.Core.Events;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Infrastructure.Kafka;
using Microsoft.Extensions.DependencyInjection;
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
        using var scope = _serviceProvider.CreateScope();
        var invoiceRepo = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditRepository>();
        var formatRepo = scope.ServiceProvider.GetRequiredService<IFormatRepository>();
        var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var invoice = await invoiceRepo.GetByIdAsync(@event.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            Logger.LogWarning("Invoice {InvoiceId} not found", @event.InvoiceId);
            return;
        }

        try
        {
            await invoiceRepo.UpdateStatusAsync(invoice.Id, InvoiceStatus.Validating.ToString(), cancellationToken: cancellationToken);
            await auditRepo.InsertAuditEntryAsync(invoice.Id, InvoiceStatus.Validating.ToString(), "Validation started", null, cancellationToken);

            var hydrated = InvoiceReconstructor.Hydrate(invoice);

            var formatVersion = await formatRepo.GetActiveFormatAsync(hydrated.Buyer.Address.CountryCode, cancellationToken);
            if (formatVersion is not null)
            {
                await invoiceRepo.UpdateStatusAsync(invoice.Id, InvoiceStatus.Validating.ToString(),
                    storecoveSubmissionId: null, cancellationToken: cancellationToken);
            }

            var validationResult = await validationService.ValidateAsync(hydrated, cancellationToken);
            var validationJson = JsonSerializer.Serialize(validationResult);

            if (validationResult.IsValid)
            {
                await invoiceRepo.UpdateStatusAsync(invoice.Id, InvoiceStatus.Valid.ToString(),
                    validationResult: validationJson, cancellationToken: cancellationToken);
                await auditRepo.InsertAuditEntryAsync(invoice.Id, InvoiceStatus.Valid.ToString(), "Validation passed", null, cancellationToken);
                await eventPublisher.PublishAsync(new InvoiceValidated(@event.InvoiceId), cancellationToken);
            }
            else
            {
                await invoiceRepo.UpdateStatusAsync(invoice.Id, InvoiceStatus.Invalid.ToString(),
                    validationResult: validationJson, cancellationToken: cancellationToken);
                await auditRepo.InsertAuditEntryAsync(invoice.Id, InvoiceStatus.Invalid.ToString(),
                    $"Validation failed with {validationResult.Errors.Count} error(s)", null, cancellationToken);
                await eventPublisher.PublishAsync(new InvoiceValidationFailed(@event.InvoiceId, "Validation failed"), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating invoice {InvoiceId}", @event.InvoiceId);
            await invoiceRepo.UpdateStatusAsync(invoice.Id, InvoiceStatus.Failed.ToString(), cancellationToken: cancellationToken);
            await auditRepo.InsertAuditEntryAsync(invoice.Id, InvoiceStatus.Failed.ToString(), $"Validation error: {ex.Message}", null, cancellationToken);
            await eventPublisher.PublishAsync(new InvoiceFailed(@event.InvoiceId, ex.Message), cancellationToken);
        }
    }
}
