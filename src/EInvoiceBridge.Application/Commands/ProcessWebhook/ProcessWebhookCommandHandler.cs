using EInvoiceBridge.Core.Interfaces;
using MediatR;

namespace EInvoiceBridge.Application.Commands.ProcessWebhook;

public sealed class ProcessWebhookCommandHandler : IRequestHandler<ProcessWebhookCommand>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IAuditRepository _auditRepository;

    public ProcessWebhookCommandHandler(IInvoiceRepository invoiceRepository, IAuditRepository auditRepository)
    {
        _invoiceRepository = invoiceRepository;
        _auditRepository = auditRepository;
    }

    public async Task Handle(ProcessWebhookCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
