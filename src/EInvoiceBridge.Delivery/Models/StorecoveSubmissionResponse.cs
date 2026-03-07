using System.Text.Json.Serialization;

namespace EInvoiceBridge.Delivery.Models;

public sealed class StorecoveSubmissionResponse
{
    [JsonPropertyName("guid")]
    public string? Guid { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
