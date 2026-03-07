using System.Text.Json.Serialization;

namespace EInvoiceBridge.Delivery.Models;

public sealed class StorecoveSubmissionRequest
{
    [JsonPropertyName("legalEntityId")]
    public long LegalEntityId { get; set; }

    [JsonPropertyName("document")]
    public StorecoveDocument Document { get; set; } = new();

    [JsonPropertyName("routing")]
    public StorecoveRouting Routing { get; set; } = new();
}

public sealed class StorecoveDocument
{
    [JsonPropertyName("documentType")]
    public string DocumentType { get; set; } = "invoice";

    [JsonPropertyName("rawDocumentData")]
    public StorecoveRawDocumentData RawDocumentData { get; set; } = new();
}

public sealed class StorecoveRawDocumentData
{
    [JsonPropertyName("document")]
    public string Document { get; set; } = string.Empty;

    [JsonPropertyName("parseStrategy")]
    public string ParseStrategy { get; set; } = "ubl";
}

public sealed class StorecoveRouting
{
    [JsonPropertyName("eIdentifiers")]
    public List<StorecoveIdentifier> EIdentifiers { get; set; } = [];

    [JsonPropertyName("emails")]
    public List<string> Emails { get; set; } = [];
}

public sealed class StorecoveIdentifier
{
    [JsonPropertyName("scheme")]
    public string Scheme { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}
