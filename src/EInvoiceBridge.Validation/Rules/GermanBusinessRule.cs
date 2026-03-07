using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Validation.Rules;

public sealed class GermanBusinessRule : IValidationRule
{
    public string RuleId => "GERMAN_BIZ";
    public int Priority => 50;

    public Task<IReadOnlyList<ValidationErrorDto>> ValidateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
