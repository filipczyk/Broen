using EInvoiceBridge.Validation.Rules;
using FluentAssertions;

namespace EInvoiceBridge.Tests.Unit.Validation;

public class SchemaCompletenessRuleTests
{
    private readonly SchemaCompletenessRule _sut = new();

    [Fact]
    public async Task ValidateAsync_WithCompleteInvoice_ReturnsNoErrors()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice();

        var errors = await _sut.ValidateAsync(invoice);

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithMissingInvoiceNumber_ReturnsError()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice();
        invoice.InvoiceNumber = string.Empty;

        var errors = await _sut.ValidateAsync(invoice);

        errors.Should().ContainSingle(e => e.Field == "InvoiceNumber");
    }
}
