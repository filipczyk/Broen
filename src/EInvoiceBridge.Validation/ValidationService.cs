using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Enums;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Validation;

public sealed class ValidationService : IValidationService
{
    private readonly IEnumerable<IValidationRule> _rules;

    public ValidationService(IEnumerable<IValidationRule> rules)
    {
        _rules = rules.OrderBy(r => r.Priority);
    }

    public async Task<ValidationResultDto> ValidateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        var allErrors = new List<ValidationErrorDto>();

        foreach (var rule in _rules)
        {
            var errors = await rule.ValidateAsync(invoice, cancellationToken);
            allErrors.AddRange(errors);
        }

        return new ValidationResultDto
        {
            IsValid = !allErrors.Any(e => e.Severity == ValidationSeverity.Error),
            Errors = allErrors.Where(e => e.Severity == ValidationSeverity.Error).ToList(),
            Warnings = allErrors.Where(e => e.Severity == ValidationSeverity.Warning).ToList()
        };
    }
}
