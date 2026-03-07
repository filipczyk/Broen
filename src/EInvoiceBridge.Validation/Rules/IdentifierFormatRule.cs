using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Validation.Rules;

public sealed class IdentifierFormatRule : IValidationRule
{
    public string RuleId => "IDENTIFIER";
    public int Priority => 40;

    public Task<IReadOnlyList<ValidationErrorDto>> ValidateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
