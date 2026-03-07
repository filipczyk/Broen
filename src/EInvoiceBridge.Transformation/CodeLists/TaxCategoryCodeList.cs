namespace EInvoiceBridge.Transformation.CodeLists;

public static class TaxCategoryCodeList
{
    public const string StandardRate = "S";
    public const string ZeroRated = "Z";
    public const string Exempt = "E";
    public const string ReverseCharge = "AE";
    public const string IntraCommunity = "K";
    public const string Export = "G";
    public const string NotSubject = "O";

    public static readonly HashSet<string> ValidCodes = [StandardRate, ZeroRated, Exempt, ReverseCharge, IntraCommunity, Export, NotSubject];
}
