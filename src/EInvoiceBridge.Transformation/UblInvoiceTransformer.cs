using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Transformation;

public sealed class UblInvoiceTransformer : ITransformationService
{
    public Task<string> TransformToUblXmlAsync(Invoice invoice, FormatVersion formatVersion, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
