using Dapper;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Persistence.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IQueryLoader _queryLoader;

    public InvoiceRepository(IDbConnectionFactory connectionFactory, IQueryLoader queryLoader)
    {
        _connectionFactory = connectionFactory;
        _queryLoader = queryLoader;
    }

    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<Invoice>> GetByStatusAsync(string status, int limit = 50, int offset = 0, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Guid> InsertAsync(Guid id, string invoiceNumber, string status, Guid? formatVersionId, string rawJson, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateStatusAsync(Guid id, string status, string? validationResult = null, string? generatedXml = null, string? storecoveSubmissionId = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
