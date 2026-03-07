namespace EInvoiceBridge.Core.Models;

public sealed class TaxBreakdown
{
    public string TaxCategoryCode { get; set; } = string.Empty;
    public decimal TaxPercent { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public string? TaxExemptionReason { get; set; }
}
