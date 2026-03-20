using EInvoiceBridge.Core.Enums;
using EInvoiceBridge.Core.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EInvoiceBridge.Application.Commands.ProcessWebhook;

public sealed class ProcessWebhookCommandHandler : IRequestHandler<ProcessWebhookCommand>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<ProcessWebhookCommandHandler> _logger;

    public ProcessWebhookCommandHandler(
        IInvoiceRepository invoiceRepository,
        IAuditRepository auditRepository,
        ILogger<ProcessWebhookCommandHandler> logger)
    {
        _invoiceRepository = invoiceRepository;
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task Handle(ProcessWebhookCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetBySubmissionIdAsync(request.SubmissionId, cancellationToken);
        if (invoice is null)
        {
            _logger.LogWarning("No invoice found for Storecove submission {SubmissionId}", request.SubmissionId);
            return;
        }

        var status = request.Status.ToLowerInvariant() switch
        {
            "delivered" => InvoiceStatus.Delivered.ToString(),
            "failed" or "error" or "rejected" => InvoiceStatus.Failed.ToString(),
            _ => null
        };

        if (status is null)
        {
            _logger.LogInformation("Ignoring unrecognized Storecove status {Status} for invoice {InvoiceId}", request.Status, invoice.Id);
            return;
        }

        await _invoiceRepository.UpdateStatusAsync(invoice.Id, status, cancellationToken: cancellationToken);
        await _auditRepository.InsertAuditEntryAsync(invoice.Id, status, $"Storecove webhook: {request.Status}", null, cancellationToken);
    }
}
