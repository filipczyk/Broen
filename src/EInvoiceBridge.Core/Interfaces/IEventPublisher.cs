using EInvoiceBridge.Core.Events;

namespace EInvoiceBridge.Core.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IIntegrationEvent;
}
