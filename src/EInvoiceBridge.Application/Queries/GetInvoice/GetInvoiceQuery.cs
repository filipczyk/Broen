using EInvoiceBridge.Core.DTOs;
using MediatR;

namespace EInvoiceBridge.Application.Queries.GetInvoice;

public sealed record GetInvoiceQuery(Guid InvoiceId) : IRequest<InvoiceResponse?>;
