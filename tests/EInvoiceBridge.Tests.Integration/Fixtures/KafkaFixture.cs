using Testcontainers.Kafka;
using Xunit;

namespace EInvoiceBridge.Tests.Integration.Fixtures;

public sealed class KafkaFixture : IAsyncLifetime
{
    public KafkaContainer Container { get; } = new KafkaBuilder()
        .WithImage("confluentinc/cp-kafka:7.6.0")
        .Build();

    public string BootstrapServers => Container.GetBootstrapAddress();

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Container.DisposeAsync();
    }
}
