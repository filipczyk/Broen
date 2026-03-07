namespace EInvoiceBridge.Transformation.CodeLists;

public static class CurrencyCode
{
    public const string Euro = "EUR";
    public const string UsDollar = "USD";
    public const string PoundSterling = "GBP";
    public const string SwissFranc = "CHF";

    public static readonly HashSet<string> ValidCodes = [Euro, UsDollar, PoundSterling, SwissFranc, "SEK", "DKK", "NOK", "PLN", "CZK", "HUF", "RON", "BGN"];
}
