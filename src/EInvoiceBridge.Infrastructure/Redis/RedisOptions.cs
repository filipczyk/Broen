namespace EInvoiceBridge.Infrastructure.Redis;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = "localhost:6379";
    public int DefaultTtlMinutes { get; set; } = 60;
}
