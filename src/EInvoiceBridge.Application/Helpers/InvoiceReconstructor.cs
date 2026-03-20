using System.Text.Json;
using EInvoiceBridge.Application.Mapping;
using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Application.Helpers;

public static class InvoiceReconstructor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static Invoice Hydrate(Invoice dbInvoice)
    {
        if (string.IsNullOrEmpty(dbInvoice.RawJson))
            throw new InvalidOperationException($"Invoice {dbInvoice.Id} has no raw JSON to hydrate from.");

        var request = JsonSerializer.Deserialize<CreateInvoiceRequest>(dbInvoice.RawJson, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize raw JSON for invoice {dbInvoice.Id}.");

        var hydrated = request.ToModel();

        // Copy DB-mapped fields
        hydrated.Id = dbInvoice.Id;
        hydrated.Status = dbInvoice.Status;
        hydrated.RawJson = dbInvoice.RawJson;
        hydrated.GeneratedXml = dbInvoice.GeneratedXml;
        hydrated.ValidationResult = dbInvoice.ValidationResult;
        hydrated.StorecoveSubmissionId = dbInvoice.StorecoveSubmissionId;
        hydrated.FormatVersionId = dbInvoice.FormatVersionId;
        hydrated.CreatedAt = dbInvoice.CreatedAt;
        hydrated.UpdatedAt = dbInvoice.UpdatedAt;

        return hydrated;
    }
}
