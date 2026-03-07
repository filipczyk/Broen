namespace EInvoiceBridge.Core.DTOs;

public sealed class InvoiceResponse
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? StorecoveSubmissionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public ValidationResultDto? ValidationResult { get; set; }
}
