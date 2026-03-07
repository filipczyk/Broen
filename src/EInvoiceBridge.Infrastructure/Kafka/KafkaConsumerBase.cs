using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EInvoiceBridge.Infrastructure.Kafka;

public abstract class KafkaConsumerBase<TEvent> : BackgroundService where TEvent : class
{
    private readonly IConsumer<string, string> _consumer;
    protected readonly ILogger Logger;
    protected abstract string Topic { get; }

    protected KafkaConsumerBase(IOptions<KafkaOptions> options, ILogger logger)
    {
        Logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            GroupId = options.Value.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(Topic);

        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);
                var @event = JsonSerializer.Deserialize<TEvent>(result.Message.Value);

                if (@event is not null)
                {
                    await HandleAsync(@event, stoppingToken);
                    _consumer.Commit(result);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error consuming message from {Topic}", Topic);
            }
        }

        _consumer.Close();
    }

    protected abstract Task HandleAsync(TEvent @event, CancellationToken cancellationToken);

    public override void Dispose()
    {
        _consumer.Dispose();
        base.Dispose();
    }
}
