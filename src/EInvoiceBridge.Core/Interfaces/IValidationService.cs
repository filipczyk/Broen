using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Core.Interfaces;

public interface IValidationService
{
    Task<ValidationResultDto> ValidateAsync(Invoice invoice, CancellationToken cancellationToken = default);
}
