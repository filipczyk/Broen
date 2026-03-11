namespace EInvoiceBridge.Core.DTOs;

public sealed class InvoicePreviewResponse
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public ValidationResultDto ValidationResult { get; set; } = new();
    public string? GeneratedXml { get; set; }
}
