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
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = _queryLoader.Load("invoices/get_by_id");
        return await connection.QuerySingleOrDefaultAsync<Invoice>(sql, new { Id = id });
    }

    public async Task<IReadOnlyList<Invoice>> GetByStatusAsync(string status, int limit = 50, int offset = 0, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = _queryLoader.Load("invoices/get_by_status");
        var results = await connection.QueryAsync<Invoice>(sql, new { Status = status, Limit = limit, Offset = offset });
        return results.ToList();
    }

    public async Task<Guid> InsertAsync(Guid id, string invoiceNumber, string status, Guid? formatVersionId, string rawJson, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = _queryLoader.Load("invoices/insert_invoice");
        var now = DateTime.UtcNow;
        return await connection.ExecuteScalarAsync<Guid>(sql, new
        {
            Id = id,
            InvoiceNumber = invoiceNumber,
            Status = status,
            FormatVersionId = formatVersionId,
            RawJson = rawJson,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    public async Task UpdateStatusAsync(Guid id, string status, string? validationResult = null, string? generatedXml = null, string? storecoveSubmissionId = null, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = _queryLoader.Load("invoices/update_status");
        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            Status = status,
            ValidationResult = validationResult,
            GeneratedXml = generatedXml,
            StorecoveSubmissionId = storecoveSubmissionId,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task<Invoice?> GetBySubmissionIdAsync(string submissionId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = _queryLoader.Load("invoices/get_by_submission_id");
        return await connection.QuerySingleOrDefaultAsync<Invoice>(sql, new { SubmissionId = submissionId });
    }
}
