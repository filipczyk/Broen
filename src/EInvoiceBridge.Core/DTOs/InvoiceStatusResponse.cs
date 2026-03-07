namespace EInvoiceBridge.Core.DTOs;

public sealed class InvoiceStatusResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public List<AuditEntryDto> AuditTrail { get; set; } = [];
}

public sealed class AuditEntryDto
{
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
}
