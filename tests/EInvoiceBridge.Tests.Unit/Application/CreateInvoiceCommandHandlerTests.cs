using System.Text.Json;
using EInvoiceBridge.Application.Commands.CreateInvoice;
using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Events;
using EInvoiceBridge.Core.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace EInvoiceBridge.Tests.Unit.Application;

public class CreateInvoiceCommandHandlerTests
{
    private readonly IInvoiceRepository _invoiceRepository = Substitute.For<IInvoiceRepository>();
    private readonly IAuditRepository _auditRepository = Substitute.For<IAuditRepository>();
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();

    private CreateInvoiceCommandHandler CreateHandler() =>
        new(_invoiceRepository, _auditRepository, _eventPublisher);

    private static CreateInvoiceRequest CreateValidRequest() => new()
    {
        InvoiceNumber = "INV-2026-001",
        IssueDate = "2026-03-06",
        DueDate = "2026-04-05",
        InvoiceTypeCode = "380",
        CurrencyCode = "EUR",
        BuyerReference = "PO-2026-1234",
        Seller = new PartyDto
        {
            Name = "Seller BV",
            VatNumber = "BE0123456789",
            Address = new AddressDto { Street = "Street 1", City = "Ghent", PostalCode = "9000", CountryCode = "BE" }
        },
        Buyer = new PartyDto
        {
            Name = "Buyer GmbH",
            VatNumber = "DE123456789",
            Address = new AddressDto { Street = "Hauptstr 1", City = "Berlin", PostalCode = "10115", CountryCode = "DE" }
        },
        PaymentMeans = new PaymentMeansDto { Code = "30", Iban = "BE68539007547034" },
        Lines =
        [
            new InvoiceLineDto
            {
                LineNumber = 1,
                Description = "Widget",
                Quantity = 10,
                UnitCode = "C62",
                UnitPrice = 100m,
                TaxCategoryCode = "K",
                TaxPercent = 0
            }
        ],
        TaxExemptionReason = "Intra-community supply"
    };

    [Fact]
    public async Task Handle_InsertsInvoiceAsReceived()
    {
        var handler = CreateHandler();
        var command = new CreateInvoiceCommand(CreateValidRequest());

        await handler.Handle(command, CancellationToken.None);

        await _invoiceRepository.Received(1).InsertAsync(
            Arg.Any<Guid>(),
            Arg.Is("INV-2026-001"),
            Arg.Is("Received"),
            Arg.Is<Guid?>(x => x == null),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WritesAuditEntry()
    {
        var handler = CreateHandler();
        var command = new CreateInvoiceCommand(CreateValidRequest());

        await handler.Handle(command, CancellationToken.None);

        await _auditRepository.Received(1).InsertAuditEntryAsync(
            Arg.Any<Guid>(),
            Arg.Is("Received"),
            Arg.Is("Invoice received"),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PublishesInvoiceReceivedEvent()
    {
        var handler = CreateHandler();
        var command = new CreateInvoiceCommand(CreateValidRequest());

        await handler.Handle(command, CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<InvoiceReceived>(e => e.InvoiceNumber == "INV-2026-001"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsReceivedStatus()
    {
        var handler = CreateHandler();
        var command = new CreateInvoiceCommand(CreateValidRequest());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be("Received");
        result.InvoiceNumber.Should().Be("INV-2026-001");
        result.Id.Should().NotBeEmpty();
        result.ValidationResult.Should().BeNull();
    }
}
