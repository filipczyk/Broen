using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Core.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetByStatusAsync(string status, int limit = 50, int offset = 0, CancellationToken cancellationToken = default);
    Task<Guid> InsertAsync(Guid id, string invoiceNumber, string status, Guid? formatVersionId, string rawJson, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid id, string status, string? validationResult = null, string? generatedXml = null, string? storecoveSubmissionId = null, CancellationToken cancellationToken = default);
    Task<Invoice?> GetBySubmissionIdAsync(string submissionId, CancellationToken cancellationToken = default);
}
