namespace EInvoiceBridge.Core.Models;

public sealed class Invoice
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public string InvoiceTypeCode { get; set; } = "380";
    public string CurrencyCode { get; set; } = "EUR";
    public string BuyerReference { get; set; } = string.Empty;
    public Party Seller { get; set; } = new();
    public Party Buyer { get; set; } = new();
    public PaymentMeans PaymentMeans { get; set; } = new();
    public List<InvoiceLine> Lines { get; set; } = [];
    public string? TaxExemptionReason { get; set; }
    public string? Notes { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public string? DeliveryCountryCode { get; set; }
    public string? DeliveryCity { get; set; }
    public string? DeliveryPostalCode { get; set; }

    // DB-mapped fields
    public string Status { get; set; } = string.Empty;
    public string? RawJson { get; set; }
    public string? GeneratedXml { get; set; }
    public string? ValidationResult { get; set; }
    public string? StorecoveSubmissionId { get; set; }
    public Guid? FormatVersionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
