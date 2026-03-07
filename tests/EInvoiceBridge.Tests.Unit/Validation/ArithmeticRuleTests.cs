using EInvoiceBridge.Validation.Rules;
using FluentAssertions;

namespace EInvoiceBridge.Tests.Unit.Validation;

public class ArithmeticRuleTests
{
    private readonly ArithmeticRule _sut = new();

    [Fact(Skip = "Stub — awaiting implementation")]
    public async Task ValidateAsync_WithCorrectArithmetic_ReturnsNoErrors()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice();
        var errors = await _sut.ValidateAsync(invoice);
        errors.Should().BeEmpty();
    }
}
