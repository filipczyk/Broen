using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Interfaces;
using MediatR;

namespace EInvoiceBridge.Application.Queries.GetInvoice;

public sealed class GetInvoiceQueryHandler : IRequestHandler<GetInvoiceQuery, InvoiceResponse?>
{
    private readonly IInvoiceRepository _invoiceRepository;

    public GetInvoiceQueryHandler(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<InvoiceResponse?> Handle(GetInvoiceQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
