using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Enums;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Validation.Rules;

public sealed class VatLogicRule : IValidationRule
{
    public string RuleId => "VAT_LOGIC";
    public int Priority => 30;

    public Task<IReadOnlyList<ValidationErrorDto>> ValidateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationErrorDto>();

        var sellerCountry = invoice.Seller.Address.CountryCode;
        var buyerCountry = invoice.Buyer.Address.CountryCode;
        var isCrossBorder = !string.IsNullOrEmpty(sellerCountry)
                            && !string.IsNullOrEmpty(buyerCountry)
                            && !sellerCountry.Equals(buyerCountry, StringComparison.OrdinalIgnoreCase);

        if (!isCrossBorder)
            return Task.FromResult<IReadOnlyList<ValidationErrorDto>>(errors);

        for (var i = 0; i < invoice.Lines.Count; i++)
        {
            var line = invoice.Lines[i];
            var prefix = $"Lines[{i}]";

            if (line.TaxCategoryCode is not ("K" or "AE"))
            {
                errors.Add(Error($"{prefix}.TaxCategoryCode",
                    $"Cross-border B2B invoices must use tax category 'K' (intra-community) or 'AE' (reverse charge), got '{line.TaxCategoryCode}'."));
            }

            if (line.TaxPercent != 0)
            {
                errors.Add(Error($"{prefix}.TaxPercent",
                    "Cross-border B2B invoices must have TaxPercent = 0."));
            }
        }

        if (string.IsNullOrWhiteSpace(invoice.TaxExemptionReason))
        {
            errors.Add(Error("TaxExemptionReason",
                "TaxExemptionReason is required for cross-border B2B invoices."));
        }

        return Task.FromResult<IReadOnlyList<ValidationErrorDto>>(errors);
    }

    private ValidationErrorDto Error(string field, string message) => new()
    {
        RuleId = RuleId,
        Severity = ValidationSeverity.Error,
        Field = field,
        Message = message
    };
}
