namespace EInvoiceBridge.Transformation.CodeLists;

public static class UnitCode
{
    public const string Piece = "C62";
    public const string Day = "DAY";
    public const string Hour = "HUR";
    public const string Kilogram = "KGM";

    public static readonly HashSet<string> ValidCodes = [Piece, Day, Hour, Kilogram, "KTM", "LTR", "MTR", "MTK", "MTQ", "TNE", "SET", "XPK"];
}
