using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Validation.Rules;

public sealed class ArithmeticRule : IValidationRule
{
    public string RuleId => "ARITHMETIC";
    public int Priority => 20;

    public Task<IReadOnlyList<ValidationErrorDto>> ValidateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
