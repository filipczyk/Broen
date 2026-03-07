using System.Collections.Concurrent;
using EInvoiceBridge.Core.Interfaces;

namespace EInvoiceBridge.Persistence.Queries;

public sealed class EmbeddedQueryLoader : IQueryLoader
{
    private readonly string _basePath;
    private readonly ConcurrentDictionary<string, string> _cache = new();

    public EmbeddedQueryLoader(string basePath)
    {
        _basePath = basePath;
    }

    public string Load(string queryName)
    {
        return _cache.GetOrAdd(queryName, name =>
        {
            var filePath = Path.Combine(_basePath, $"{name}.sql");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"SQL query file not found: {filePath}");
            }
            return File.ReadAllText(filePath);
        });
    }
}
