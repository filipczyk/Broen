using EInvoiceBridge.Application.Commands.ProcessWebhook;
using EInvoiceBridge.Core.Models;
using EInvoiceBridge.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace EInvoiceBridge.Tests.Unit.Application;

public class ProcessWebhookCommandHandlerTests
{
    private readonly IInvoiceRepository _invoiceRepository = Substitute.For<IInvoiceRepository>();
    private readonly IAuditRepository _auditRepository = Substitute.For<IAuditRepository>();
    private readonly ILogger<ProcessWebhookCommandHandler> _logger = Substitute.For<ILogger<ProcessWebhookCommandHandler>>();

    private ProcessWebhookCommandHandler CreateHandler() =>
        new(_invoiceRepository, _auditRepository, _logger);

    [Fact]
    public async Task Handle_DeliveredStatus_UpdatesToDelivered()
    {
        var invoice = new Invoice { Id = Guid.NewGuid(), StorecoveSubmissionId = "sub-123" };
        _invoiceRepository.GetBySubmissionIdAsync("sub-123", Arg.Any<CancellationToken>()).Returns(invoice);

        var handler = CreateHandler();
        var command = new ProcessWebhookCommand("sub-123", "delivered", DateTime.UtcNow);

        await handler.Handle(command, CancellationToken.None);

        await _invoiceRepository.Received(1).UpdateStatusAsync(invoice.Id, "Delivered",
            validationResult: Arg.Any<string?>(), generatedXml: Arg.Any<string?>(),
            storecoveSubmissionId: Arg.Any<string?>(), cancellationToken: Arg.Any<CancellationToken>());
        await _auditRepository.Received(1).InsertAuditEntryAsync(invoice.Id, "Delivered",
            "Storecove webhook: delivered", Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FailedStatus_UpdatesToFailed()
    {
        var invoice = new Invoice { Id = Guid.NewGuid(), StorecoveSubmissionId = "sub-456" };
        _invoiceRepository.GetBySubmissionIdAsync("sub-456", Arg.Any<CancellationToken>()).Returns(invoice);

        var handler = CreateHandler();
        var command = new ProcessWebhookCommand("sub-456", "failed", DateTime.UtcNow);

        await handler.Handle(command, CancellationToken.None);

        await _invoiceRepository.Received(1).UpdateStatusAsync(invoice.Id, "Failed",
            validationResult: Arg.Any<string?>(), generatedXml: Arg.Any<string?>(),
            storecoveSubmissionId: Arg.Any<string?>(), cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownSubmissionId_DoesNothing()
    {
        _invoiceRepository.GetBySubmissionIdAsync("unknown", Arg.Any<CancellationToken>()).Returns((Invoice?)null);

        var handler = CreateHandler();
        var command = new ProcessWebhookCommand("unknown", "delivered", DateTime.UtcNow);

        await handler.Handle(command, CancellationToken.None);

        await _invoiceRepository.DidNotReceive().UpdateStatusAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownStatus_DoesNothing()
    {
        var invoice = new Invoice { Id = Guid.NewGuid(), StorecoveSubmissionId = "sub-789" };
        _invoiceRepository.GetBySubmissionIdAsync("sub-789", Arg.Any<CancellationToken>()).Returns(invoice);

        var handler = CreateHandler();
        var command = new ProcessWebhookCommand("sub-789", "processing", DateTime.UtcNow);

        await handler.Handle(command, CancellationToken.None);

        await _invoiceRepository.DidNotReceive().UpdateStatusAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
