using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Enums;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Validation.Rules;

public sealed class GermanBusinessRule : IValidationRule
{
    public string RuleId => "GERMAN_BIZ";
    public int Priority => 50;

    public Task<IReadOnlyList<ValidationErrorDto>> ValidateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationErrorDto>();

        if (invoice.Buyer.Address.CountryCode.Equals("DE", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(invoice.BuyerReference))
        {
            errors.Add(new ValidationErrorDto
            {
                RuleId = RuleId,
                Severity = ValidationSeverity.Error,
                Field = "BuyerReference",
                Message = "BuyerReference (BT-10) is mandatory for XRechnung when buyer is in Germany."
            });
        }

        return Task.FromResult<IReadOnlyList<ValidationErrorDto>>(errors);
    }
}
