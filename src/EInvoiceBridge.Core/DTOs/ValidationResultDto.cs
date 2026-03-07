using EInvoiceBridge.Core.Enums;

namespace EInvoiceBridge.Core.DTOs;

public sealed class ValidationResultDto
{
    public bool IsValid { get; set; }
    public List<ValidationErrorDto> Errors { get; set; } = [];
    public List<ValidationErrorDto> Warnings { get; set; } = [];
}

public sealed class ValidationErrorDto
{
    public string RuleId { get; set; } = string.Empty;
    public ValidationSeverity Severity { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
