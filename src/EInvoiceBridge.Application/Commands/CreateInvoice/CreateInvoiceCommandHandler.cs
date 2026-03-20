using System.Text.Json;
using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Enums;
using EInvoiceBridge.Core.Events;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Application.Mapping;
using MediatR;

namespace EInvoiceBridge.Application.Commands.CreateInvoice;

public sealed class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, InvoiceResponse>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IEventPublisher _eventPublisher;

    public CreateInvoiceCommandHandler(
        IInvoiceRepository invoiceRepository,
        IAuditRepository auditRepository,
        IEventPublisher eventPublisher)
    {
        _invoiceRepository = invoiceRepository;
        _auditRepository = auditRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<InvoiceResponse> Handle(CreateInvoiceCommand command, CancellationToken cancellationToken)
    {
        var invoice = command.Request.ToModel();
        var rawJson = JsonSerializer.Serialize(command.Request);

        var status = InvoiceStatus.Received.ToString();
        await _invoiceRepository.InsertAsync(invoice.Id, invoice.InvoiceNumber, status, null, rawJson, cancellationToken);
        await _auditRepository.InsertAuditEntryAsync(invoice.Id, status, "Invoice received", null, cancellationToken);

        await _eventPublisher.PublishAsync(new InvoiceReceived(invoice.Id, invoice.InvoiceNumber), cancellationToken);

        return new InvoiceResponse
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
    }
}
