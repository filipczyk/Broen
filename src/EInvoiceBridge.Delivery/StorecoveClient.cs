using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/document_submissions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<StorecoveSubmissionResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Storecove returned null response.");
    }
}
