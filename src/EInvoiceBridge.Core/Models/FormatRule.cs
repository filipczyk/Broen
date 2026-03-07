namespace EInvoiceBridge.Core.Models;

public sealed class FormatRule
{
    public Guid Id { get; set; }
    public Guid FormatVersionId { get; set; }
    public string RuleType { get; set; } = string.Empty;
    public string RuleKey { get; set; } = string.Empty;
    public string? RuleConfig { get; set; }
    public int Priority { get; set; }
    public bool IsEnabled { get; set; } = true;
}
