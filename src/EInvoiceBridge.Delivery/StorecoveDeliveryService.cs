using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Delivery.Options;
using Microsoft.Extensions.Options;

namespace EInvoiceBridge.Delivery;

public sealed class StorecoveDeliveryService : IDeliveryService
{
    private readonly StorecoveClient _client;
    private readonly StorecoveOptions _options;

    public StorecoveDeliveryService(StorecoveClient client, IOptions<StorecoveOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<string> SubmitAsync(Guid invoiceId, string ublXml, string buyerVatNumber, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
