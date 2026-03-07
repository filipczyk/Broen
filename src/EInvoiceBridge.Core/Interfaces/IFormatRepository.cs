using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Core.Interfaces;

public interface IFormatRepository
{
    Task<FormatVersion?> GetActiveFormatAsync(string countryCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormatRule>> GetRulesByFormatAsync(Guid formatVersionId, string ruleType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CodeListEntry>> GetCodeListAsync(string listType, Guid? formatVersionId = null, CancellationToken cancellationToken = default);
}
