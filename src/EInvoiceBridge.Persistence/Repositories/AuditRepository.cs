using Dapper;
using EInvoiceBridge.Core.Interfaces;

namespace EInvoiceBridge.Persistence.Repositories;

public sealed class AuditRepository : IAuditRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IQueryLoader _queryLoader;

    public AuditRepository(IDbConnectionFactory connectionFactory, IQueryLoader queryLoader)
    {
        _connectionFactory = connectionFactory;
        _queryLoader = queryLoader;
    }

    public async Task InsertAuditEntryAsync(Guid invoiceId, string status, string? message = null, string? details = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
