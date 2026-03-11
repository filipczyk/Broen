using System.Text.Json;
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
        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
            return null;

        ValidationResultDto? validationResult = null;
        if (!string.IsNullOrEmpty(invoice.ValidationResult))
        {
            validationResult = JsonSerializer.Deserialize<ValidationResultDto>(invoice.ValidationResult);
        }

        return new InvoiceResponse
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Status = invoice.Status,
            StorecoveSubmissionId = invoice.StorecoveSubmissionId,
            CreatedAt = invoice.CreatedAt,
            ValidationResult = validationResult
        };
    }
}
