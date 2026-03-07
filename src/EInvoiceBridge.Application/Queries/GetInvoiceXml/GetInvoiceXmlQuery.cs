using MediatR;

namespace EInvoiceBridge.Application.Queries.GetInvoiceXml;

public sealed record GetInvoiceXmlQuery(Guid InvoiceId) : IRequest<string?>;
