using Dapper;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Persistence.Repositories;

public sealed class FormatRepository : IFormatRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IQueryLoader _queryLoader;

    public FormatRepository(IDbConnectionFactory connectionFactory, IQueryLoader queryLoader)
    {
        _connectionFactory = connectionFactory;
        _queryLoader = queryLoader;
    }

    public async Task<FormatVersion?> GetActiveFormatAsync(string countryCode, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<FormatRule>> GetRulesByFormatAsync(Guid formatVersionId, string ruleType, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<CodeListEntry>> GetCodeListAsync(string listType, Guid? formatVersionId = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
