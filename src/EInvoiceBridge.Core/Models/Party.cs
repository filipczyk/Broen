namespace EInvoiceBridge.Core.Models;

public sealed class Party
{
    public string Name { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public Address Address { get; set; } = new();
    public Contact? Contact { get; set; }
}
