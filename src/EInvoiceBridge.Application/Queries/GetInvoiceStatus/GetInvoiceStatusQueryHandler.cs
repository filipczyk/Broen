using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Interfaces;
using MediatR;

namespace EInvoiceBridge.Application.Queries.GetInvoiceStatus;

public sealed class GetInvoiceStatusQueryHandler : IRequestHandler<GetInvoiceStatusQuery, InvoiceStatusResponse?>
{
    private readonly IInvoiceRepository _invoiceRepository;

    public GetInvoiceStatusQueryHandler(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<InvoiceStatusResponse?> Handle(GetInvoiceStatusQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
            return null;

        return new InvoiceStatusResponse
        {
            Id = invoice.Id,
            Status = invoice.Status,
            UpdatedAt = invoice.UpdatedAt
        };
    }
}
