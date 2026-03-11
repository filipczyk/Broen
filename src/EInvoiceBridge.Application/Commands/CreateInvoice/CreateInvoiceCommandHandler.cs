using System.Text.Json;
using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Enums;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Application.Mapping;
using MediatR;

namespace EInvoiceBridge.Application.Commands.CreateInvoice;

public sealed class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, InvoiceResponse>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IFormatRepository _formatRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IValidationService _validationService;
    private readonly ITransformationService _transformationService;

    public CreateInvoiceCommandHandler(
        IInvoiceRepository invoiceRepository,
        IFormatRepository formatRepository,
        IAuditRepository auditRepository,
        IValidationService validationService,
        ITransformationService transformationService)
    {
        _invoiceRepository = invoiceRepository;
        _formatRepository = formatRepository;
        _auditRepository = auditRepository;
        _validationService = validationService;
        _transformationService = transformationService;
    }

    public async Task<InvoiceResponse> Handle(CreateInvoiceCommand command, CancellationToken cancellationToken)
    {
        var invoice = command.Request.ToModel();
        var rawJson = JsonSerializer.Serialize(command.Request);

        // Look up active format for buyer country
        var buyerCountry = invoice.Buyer.Address.CountryCode;
        var formatVersion = await _formatRepository.GetActiveFormatAsync(buyerCountry, cancellationToken);

        // Insert invoice as Received
        var status = InvoiceStatus.Received.ToString();
        await _invoiceRepository.InsertAsync(invoice.Id, invoice.InvoiceNumber, status, formatVersion?.Id, rawJson, cancellationToken);
        await _auditRepository.InsertAuditEntryAsync(invoice.Id, status, "Invoice received", null, cancellationToken);

        // Validate
        status = InvoiceStatus.Validating.ToString();
        await _invoiceRepository.UpdateStatusAsync(invoice.Id, status, cancellationToken: cancellationToken);

        var validationResult = await _validationService.ValidateAsync(invoice, cancellationToken);
        var validationJson = JsonSerializer.Serialize(validationResult);

        if (validationResult.IsValid)
        {
            status = InvoiceStatus.Valid.ToString();
            await _invoiceRepository.UpdateStatusAsync(invoice.Id, status, validationResult: validationJson, cancellationToken: cancellationToken);
            await _auditRepository.InsertAuditEntryAsync(invoice.Id, status, "Validation passed", null, cancellationToken);

            // Transform to UBL XML if format available
            if (formatVersion is not null)
            {
                status = InvoiceStatus.Transforming.ToString();
                await _invoiceRepository.UpdateStatusAsync(invoice.Id, status, cancellationToken: cancellationToken);

                var xml = await _transformationService.TransformToUblXmlAsync(invoice, formatVersion, cancellationToken);
                await _invoiceRepository.UpdateStatusAsync(invoice.Id, status, generatedXml: xml, cancellationToken: cancellationToken);
                await _auditRepository.InsertAuditEntryAsync(invoice.Id, status, "UBL XML generated", null, cancellationToken);
            }
        }
        else
        {
            status = InvoiceStatus.Invalid.ToString();
            await _invoiceRepository.UpdateStatusAsync(invoice.Id, status, validationResult: validationJson, cancellationToken: cancellationToken);
            await _auditRepository.InsertAuditEntryAsync(invoice.Id, status,
                $"Validation failed with {validationResult.Errors.Count} error(s)", null, cancellationToken);
        }

        return new InvoiceResponse
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            ValidationResult = validationResult
        };
    }
}
