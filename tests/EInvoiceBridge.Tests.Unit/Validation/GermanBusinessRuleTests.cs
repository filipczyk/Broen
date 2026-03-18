using EInvoiceBridge.Validation.Rules;
using FluentAssertions;

namespace EInvoiceBridge.Tests.Unit.Validation;

public class GermanBusinessRuleTests
{
    private readonly GermanBusinessRule _sut = new();

    [Fact]
    public async Task ValidateAsync_WithBuyerReference_ReturnsNoErrors()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice();
        var errors = await _sut.ValidateAsync(invoice);
        errors.Should().BeEmpty();
    }
}
