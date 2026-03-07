using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Interfaces;
using MediatR;

namespace EInvoiceBridge.Application.Commands.CreateInvoice;

public sealed class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, InvoiceResponse>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IFormatRepository _formatRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IEventPublisher _eventPublisher;

    public CreateInvoiceCommandHandler(
        IInvoiceRepository invoiceRepository,
        IFormatRepository formatRepository,
        IAuditRepository auditRepository,
        IEventPublisher eventPublisher)
    {
        _invoiceRepository = invoiceRepository;
        _formatRepository = formatRepository;
        _auditRepository = auditRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<InvoiceResponse> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
