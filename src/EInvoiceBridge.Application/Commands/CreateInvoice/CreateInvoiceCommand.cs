using EInvoiceBridge.Core.DTOs;
using MediatR;

namespace EInvoiceBridge.Application.Commands.CreateInvoice;

public sealed record CreateInvoiceCommand(CreateInvoiceRequest Request) : IRequest<InvoiceResponse>;
