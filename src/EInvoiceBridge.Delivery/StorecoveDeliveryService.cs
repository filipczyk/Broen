using System.Text;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Delivery.Models;
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
        var base64Xml = Convert.ToBase64String(Encoding.UTF8.GetBytes(ublXml));

        var countryPrefix = buyerVatNumber.Length >= 2 ? buyerVatNumber[..2].ToUpperInvariant() : "EU";
        var scheme = MapCountryToScheme(countryPrefix);

        var request = new StorecoveSubmissionRequest
        {
            LegalEntityId = _options.LegalEntityId,
            Document = new StorecoveDocument
            {
                DocumentType = "invoice",
                RawDocumentData = new StorecoveRawDocumentData
                {
                    Document = base64Xml,
                    ParseStrategy = "ubl"
                }
            },
            Routing = new StorecoveRouting
            {
                EIdentifiers =
                [
                    new StorecoveIdentifier
                    {
                        Scheme = scheme,
                        Id = buyerVatNumber
                    }
                ]
            }
        };

        var response = await _client.SubmitDocumentAsync(request, cancellationToken);
        return response.Guid ?? response.Id ?? throw new InvalidOperationException("Storecove response contained no identifier.");
    }

    private static string MapCountryToScheme(string countryCode) => countryCode switch
    {
        "DE" => "DE:VAT",
        "BE" => "BE:EN",
        "FR" => "FR:SIRET",
        "IT" => "IT:VAT",
        _ => "EU:VAT"
    };
}
