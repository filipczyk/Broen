using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Core.Interfaces;

public interface ITransformationService
{
    Task<string> TransformToUblXmlAsync(Invoice invoice, FormatVersion formatVersion, CancellationToken cancellationToken = default);
}
