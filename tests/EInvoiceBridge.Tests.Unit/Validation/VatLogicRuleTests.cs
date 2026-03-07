using EInvoiceBridge.Validation.Rules;
using FluentAssertions;

namespace EInvoiceBridge.Tests.Unit.Validation;

public class VatLogicRuleTests
{
    private readonly VatLogicRule _sut = new();

    [Fact(Skip = "Stub — awaiting implementation")]
    public async Task ValidateAsync_CrossBorderWithCategoryK_ReturnsNoErrors()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice();
        var errors = await _sut.ValidateAsync(invoice);
        errors.Should().BeEmpty();
    }
}
