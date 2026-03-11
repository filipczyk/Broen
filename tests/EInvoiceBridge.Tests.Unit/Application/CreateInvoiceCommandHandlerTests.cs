using EInvoiceBridge.Application.Commands.CreateInvoice;
using EInvoiceBridge.Core.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace EInvoiceBridge.Tests.Unit.Application;

public class CreateInvoiceCommandHandlerTests
{
    private readonly IInvoiceRepository _invoiceRepository = Substitute.For<IInvoiceRepository>();
    private readonly IFormatRepository _formatRepository = Substitute.For<IFormatRepository>();
    private readonly IAuditRepository _auditRepository = Substitute.For<IAuditRepository>();
    private readonly IValidationService _validationService = Substitute.For<IValidationService>();
    private readonly ITransformationService _transformationService = Substitute.For<ITransformationService>();

    [Fact(Skip = "Stub — awaiting implementation")]
    public async Task Handle_WithValidRequest_ReturnsInvoiceResponse()
    {
        // Arrange
        var handler = new CreateInvoiceCommandHandler(
            _invoiceRepository, _formatRepository, _auditRepository,
            _validationService, _transformationService);

        // Act & Assert
        // TODO: Implement when handler is implemented
    }
}
