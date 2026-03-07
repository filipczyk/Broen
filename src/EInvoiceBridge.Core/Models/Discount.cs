namespace EInvoiceBridge.Core.Models;

public sealed class Discount
{
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}
