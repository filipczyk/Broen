namespace EInvoiceBridge.Core.Models;

public sealed class InvoiceLine
{
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitCode { get; set; } = "C62";
    public decimal UnitPrice { get; set; }
    public Discount? Discount { get; set; }
    public string TaxCategoryCode { get; set; } = string.Empty;
    public decimal TaxPercent { get; set; }
}
