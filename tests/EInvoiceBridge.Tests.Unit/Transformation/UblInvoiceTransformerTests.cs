using EInvoiceBridge.Core.Models;
using EInvoiceBridge.Transformation;
using FluentAssertions;

namespace EInvoiceBridge.Tests.Unit.Transformation;

public class UblInvoiceTransformerTests
{
    private readonly UblInvoiceTransformer _sut = new();

    [Fact(Skip = "Stub — awaiting implementation")]
    public async Task TransformToUblXmlAsync_WithValidInvoice_ReturnsValidXml()
    {
        // Arrange
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice();
        var formatVersion = new FormatVersion
        {
            Id = Guid.NewGuid(),
            CustomizationId = "urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0",
            ProfileId = "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0"
        };

        // Act
        var xml = await _sut.TransformToUblXmlAsync(invoice, formatVersion);

        // Assert
        xml.Should().NotBeNullOrEmpty();
        xml.Should().Contain("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2");
    }
}
