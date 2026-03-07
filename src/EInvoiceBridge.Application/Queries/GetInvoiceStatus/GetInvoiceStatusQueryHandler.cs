using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Interfaces;
using MediatR;

namespace EInvoiceBridge.Application.Queries.GetInvoiceStatus;

public sealed class GetInvoiceStatusQueryHandler : IRequestHandler<GetInvoiceStatusQuery, InvoiceStatusResponse?>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IAuditRepository _auditRepository;

    public GetInvoiceStatusQueryHandler(IInvoiceRepository invoiceRepository, IAuditRepository auditRepository)
    {
        _invoiceRepository = invoiceRepository;
        _auditRepository = auditRepository;
    }

    public async Task<InvoiceStatusResponse?> Handle(GetInvoiceStatusQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
