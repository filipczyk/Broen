using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Enums;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Validation.Rules;

public sealed class ArithmeticRule : IValidationRule
{
    public string RuleId => "ARITHMETIC";
    public int Priority => 20;

    public Task<IReadOnlyList<ValidationErrorDto>> ValidateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationErrorDto>();

        for (var i = 0; i < invoice.Lines.Count; i++)
        {
            var line = invoice.Lines[i];
            var prefix = $"Lines[{i}]";

            if (line.Quantity <= 0)
                errors.Add(Error($"{prefix}.Quantity", "Quantity must be greater than zero."));

            if (line.UnitPrice < 0)
                errors.Add(Error($"{prefix}.UnitPrice", "UnitPrice must be zero or positive."));

            if (line.Discount is not null && line.Discount.Amount < 0)
                errors.Add(Error($"{prefix}.Discount.Amount", "Discount amount must be zero or positive."));
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
