using EInvoiceBridge.Delivery.Models;
using EInvoiceBridge.Delivery.Options;
using Microsoft.Extensions.Options;

namespace EInvoiceBridge.Delivery;

public sealed class StorecoveClient
{
    private readonly HttpClient _httpClient;
    private readonly StorecoveOptions _options;

    public StorecoveClient(HttpClient httpClient, IOptions<StorecoveOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<StorecoveSubmissionResponse> SubmitDocumentAsync(StorecoveSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
