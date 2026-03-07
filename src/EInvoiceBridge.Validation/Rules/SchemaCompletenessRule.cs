using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Validation.Rules;

public sealed class SchemaCompletenessRule : IValidationRule
{
    public string RuleId => "SCHEMA";
    public int Priority => 10;

    public Task<IReadOnlyList<ValidationErrorDto>> ValidateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
