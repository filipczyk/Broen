using System.Text.RegularExpressions;
using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Enums;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Validation.Rules;

public sealed partial class IdentifierFormatRule : IValidationRule
{
    public string RuleId => "IDENTIFIER";
    public int Priority => 40;

    [GeneratedRegex(@"^[A-Z]{2}\d+$")]
    private static partial Regex VatNumberPattern();

    public Task<IReadOnlyList<ValidationErrorDto>> ValidateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationErrorDto>();

        if (!string.IsNullOrWhiteSpace(invoice.Seller.VatNumber) && !VatNumberPattern().IsMatch(invoice.Seller.VatNumber))
        {
            errors.Add(Error("Seller.VatNumber",
                $"VAT number '{invoice.Seller.VatNumber}' must start with a 2-letter country code followed by digits."));
        }

        if (!string.IsNullOrWhiteSpace(invoice.Buyer.VatNumber) && !VatNumberPattern().IsMatch(invoice.Buyer.VatNumber))
        {
            errors.Add(Error("Buyer.VatNumber",
                $"VAT number '{invoice.Buyer.VatNumber}' must start with a 2-letter country code followed by digits."));
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
