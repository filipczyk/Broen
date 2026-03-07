namespace EInvoiceBridge.Core.Events;

public sealed record InvoiceReceived(Guid InvoiceId, string InvoiceNumber) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string EventType => "invoice.received";
}

public sealed record InvoiceValidated(Guid InvoiceId) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string EventType => "invoice.validated";
}

public sealed record InvoiceValidationFailed(Guid InvoiceId, string Reason) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string EventType => "invoice.validation-failed";
}

public sealed record InvoiceTransformed(Guid InvoiceId) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string EventType => "invoice.transformed";
}

public sealed record InvoiceSent(Guid InvoiceId, string SubmissionId) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string EventType => "invoice.sent";
}

public sealed record InvoiceDelivered(Guid InvoiceId) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string EventType => "invoice.delivered";
}

public sealed record InvoiceFailed(Guid InvoiceId, string Reason) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string EventType => "invoice.failed";
}
