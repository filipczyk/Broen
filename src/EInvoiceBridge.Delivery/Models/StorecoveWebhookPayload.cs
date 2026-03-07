using System.Text.Json.Serialization;

namespace EInvoiceBridge.Delivery.Models;

public sealed class StorecoveWebhookPayload
{
    [JsonPropertyName("document_submission_id")]
    public string DocumentSubmissionId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("details")]
    public Dictionary<string, object>? Details { get; set; }
}
