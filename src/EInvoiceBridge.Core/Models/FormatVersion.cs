namespace EInvoiceBridge.Core.Models;

public sealed class FormatVersion
{
    public Guid Id { get; set; }
    public Guid FormatDefinitionId { get; set; }
    public string FormatName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string? CustomizationId { get; set; }
    public string? ProfileId { get; set; }
    public string Status { get; set; } = "draft";
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveUntil { get; set; }
    public string? SchemaPath { get; set; }
}
