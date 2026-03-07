namespace EInvoiceBridge.Delivery.Options;

public sealed class StorecoveOptions
{
    public const string SectionName = "Storecove";

    public string BaseUrl { get; set; } = "https://api.storecove.com/api/v2";
    public string ApiKey { get; set; } = string.Empty;
    public long LegalEntityId { get; set; }
    public string WebhookSecret { get; set; } = string.Empty;
}
