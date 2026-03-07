namespace EInvoiceBridge.Transformation.CodeLists;

public static class InvoiceTypeCodeList
{
    public const string CommercialInvoice = "380";
    public const string CreditNote = "381";

    public static readonly HashSet<string> ValidCodes = [CommercialInvoice, CreditNote];
}
