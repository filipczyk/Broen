namespace EInvoiceBridge.Core.Interfaces;

public interface IAuditRepository
{
    Task InsertAuditEntryAsync(Guid invoiceId, string status, string? message = null, string? details = null, CancellationToken cancellationToken = default);
}
