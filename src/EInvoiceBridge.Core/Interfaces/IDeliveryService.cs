namespace EInvoiceBridge.Core.Interfaces;

public interface IDeliveryService
{
    Task<string> SubmitAsync(Guid invoiceId, string ublXml, string buyerVatNumber, CancellationToken cancellationToken = default);
}
