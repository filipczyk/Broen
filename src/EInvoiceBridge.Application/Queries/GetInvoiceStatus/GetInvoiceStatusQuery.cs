using EInvoiceBridge.Core.DTOs;
using MediatR;

namespace EInvoiceBridge.Application.Queries.GetInvoiceStatus;

public sealed record GetInvoiceStatusQuery(Guid InvoiceId) : IRequest<InvoiceStatusResponse?>;
