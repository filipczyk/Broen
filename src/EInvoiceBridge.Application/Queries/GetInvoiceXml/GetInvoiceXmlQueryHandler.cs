using EInvoiceBridge.Core.Interfaces;
using MediatR;

namespace EInvoiceBridge.Application.Queries.GetInvoiceXml;

public sealed class GetInvoiceXmlQueryHandler : IRequestHandler<GetInvoiceXmlQuery, string?>
{
    private readonly IInvoiceRepository _invoiceRepository;

    public GetInvoiceXmlQueryHandler(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<string?> Handle(GetInvoiceXmlQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        return invoice?.GeneratedXml;
    }
}
