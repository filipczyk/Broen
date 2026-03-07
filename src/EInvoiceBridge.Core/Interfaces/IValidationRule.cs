using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Core.Interfaces;

public interface IValidationRule
{
    string RuleId { get; }
    int Priority { get; }
    Task<IReadOnlyList<ValidationErrorDto>> ValidateAsync(Invoice invoice, CancellationToken cancellationToken = default);
}
