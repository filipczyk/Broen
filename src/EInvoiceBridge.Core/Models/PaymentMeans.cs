namespace EInvoiceBridge.Core.Models;

public sealed class PaymentMeans
{
    public string Code { get; set; } = "30";
    public string Iban { get; set; } = string.Empty;
    public string? Bic { get; set; }
}
