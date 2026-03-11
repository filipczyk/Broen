using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Enums;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Core.Models;
using EInvoiceBridge.Application.Mapping;
using MediatR;

namespace EInvoiceBridge.Application.Commands.PreviewInvoice;

public sealed class PreviewInvoiceCommandHandler : IRequestHandler<PreviewInvoiceCommand, InvoicePreviewResponse>
{
    private readonly IValidationService _validationService;
    private readonly ITransformationService _transformationService;
    private readonly IFormatRepository _formatRepository;

    public PreviewInvoiceCommandHandler(
        IValidationService validationService,
        ITransformationService transformationService,
        IFormatRepository formatRepository)
    {
        _validationService = validationService;
        _transformationService = transformationService;
        _formatRepository = formatRepository;
    }

    public async Task<InvoicePreviewResponse> Handle(PreviewInvoiceCommand command, CancellationToken cancellationToken)
    {
        var invoice = command.Request.ToModel();

        var validationResult = await _validationService.ValidateAsync(invoice, cancellationToken);

        var response = new InvoicePreviewResponse
        {
            InvoiceNumber = invoice.InvoiceNumber,
            Status = validationResult.IsValid
                ? InvoiceStatus.Valid.ToString()
                : InvoiceStatus.Invalid.ToString(),
            ValidationResult = validationResult
        };

        if (validationResult.IsValid)
        {
            var formatVersion = await GetFormatVersionAsync(invoice.Buyer.Address.CountryCode, cancellationToken);
            response.GeneratedXml = await _transformationService.TransformToUblXmlAsync(invoice, formatVersion, cancellationToken);
        }

        return response;
    }

    private async Task<FormatVersion> GetFormatVersionAsync(string countryCode, CancellationToken cancellationToken)
    {
        try
        {
            var format = await _formatRepository.GetActiveFormatAsync(countryCode, cancellationToken);
            if (format is not null)
                return format;
        }
        catch
        {
            // DB unavailable — fall back to hardcoded defaults
        }

        return new FormatVersion
        {
            Id = Guid.Empty,
            FormatName = "XRechnung",
            Version = "3.0",
            CountryCode = countryCode,
            CustomizationId = "urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0",
            ProfileId = "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0",
            Status = "active"
        };
    }
}
