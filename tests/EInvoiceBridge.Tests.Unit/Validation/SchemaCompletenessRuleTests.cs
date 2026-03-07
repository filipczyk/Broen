using EInvoiceBridge.Validation.Rules;
using FluentAssertions;

namespace EInvoiceBridge.Tests.Unit.Validation;

public class SchemaCompletenessRuleTests
{
    private readonly SchemaCompletenessRule _sut = new();

    [Fact(Skip = "Stub — awaiting implementation")]
    public async Task ValidateAsync_WithCompleteInvoice_ReturnsNoErrors()
    {
        // Arrange
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice();

        // Act
        var errors = await _sut.ValidateAsync(invoice);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact(Skip = "Stub — awaiting implementation")]
    public async Task ValidateAsync_WithMissingInvoiceNumber_ReturnsError()
    {
        // Arrange
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice();
        invoice.InvoiceNumber = string.Empty;

        // Act
        var errors = await _sut.ValidateAsync(invoice);

        // Assert
        errors.Should().ContainSingle(e => e.Field == "invoiceNumber");
    }
}
