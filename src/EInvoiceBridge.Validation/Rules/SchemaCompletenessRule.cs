using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Enums;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Validation.Rules;

public sealed class SchemaCompletenessRule : IValidationRule
{
    public string RuleId => "SCHEMA";
    public int Priority => 10;

    public Task<IReadOnlyList<ValidationErrorDto>> ValidateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationErrorDto>();

        RequireNotEmpty(errors, invoice.InvoiceNumber, "InvoiceNumber");
        if (invoice.IssueDate == default)
            errors.Add(Error("IssueDate", "IssueDate is required."));
        if (invoice.DueDate == default)
            errors.Add(Error("DueDate", "DueDate is required."));
        RequireNotEmpty(errors, invoice.BuyerReference, "BuyerReference");

        // Seller
        RequireNotEmpty(errors, invoice.Seller.Name, "Seller.Name");
        RequireNotEmpty(errors, invoice.Seller.VatNumber, "Seller.VatNumber");
        RequireNotEmpty(errors, invoice.Seller.Address.CountryCode, "Seller.Address.CountryCode");

        // Buyer
        RequireNotEmpty(errors, invoice.Buyer.Name, "Buyer.Name");
        RequireNotEmpty(errors, invoice.Buyer.VatNumber, "Buyer.VatNumber");
        RequireNotEmpty(errors, invoice.Buyer.Address.CountryCode, "Buyer.Address.CountryCode");

        // Lines
        if (invoice.Lines.Count == 0)
        {
            errors.Add(Error("Lines", "At least one invoice line is required."));
        }
        else
        {
            for (var i = 0; i < invoice.Lines.Count; i++)
            {
                var line = invoice.Lines[i];
                var prefix = $"Lines[{i}]";
                RequireNotEmpty(errors, line.Description, $"{prefix}.Description");
                if (line.Quantity == 0)
                    errors.Add(Error($"{prefix}.Quantity", "Quantity is required."));
                RequireNotEmpty(errors, line.TaxCategoryCode, $"{prefix}.TaxCategoryCode");
            }
        }

        return Task.FromResult<IReadOnlyList<ValidationErrorDto>>(errors);
    }

    private void RequireNotEmpty(List<ValidationErrorDto> errors, string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(Error(field, $"{field} is required."));
    }

    private ValidationErrorDto Error(string field, string message) => new()
    {
        RuleId = RuleId,
        Severity = ValidationSeverity.Error,
        Field = field,
        Message = message
    };
}
