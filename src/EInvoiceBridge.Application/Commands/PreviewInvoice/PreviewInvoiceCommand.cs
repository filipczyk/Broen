using EInvoiceBridge.Core.DTOs;
using MediatR;

namespace EInvoiceBridge.Application.Commands.PreviewInvoice;

public sealed record PreviewInvoiceCommand(CreateInvoiceRequest Request) : IRequest<InvoicePreviewResponse>;
