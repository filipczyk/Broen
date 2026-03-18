using EInvoiceBridge.Validation.Rules;
using FluentAssertions;

namespace EInvoiceBridge.Tests.Unit.Validation;

public class IdentifierFormatRuleTests
{
    private readonly IdentifierFormatRule _sut = new();

    [Fact]
    public async Task ValidateAsync_WithValidIdentifiers_ReturnsNoErrors()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice();
        var errors = await _sut.ValidateAsync(invoice);
        errors.Should().BeEmpty();
    }
}
