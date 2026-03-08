using System.Xml.Linq;
using EInvoiceBridge.Core.Models;
using EInvoiceBridge.Transformation;
using FluentAssertions;

namespace EInvoiceBridge.Tests.Unit.Transformation;

public class UblInvoiceTransformerTests
{
    private static readonly XNamespace Cac = XmlNamespaces.Cac;
    private static readonly XNamespace Cbc = XmlNamespaces.Cbc;

    private readonly UblInvoiceTransformer _sut = new();

    private static FormatVersion DefaultFormatVersion => new()
    {
        Id = Guid.NewGuid(),
        CustomizationId = "urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0",
        ProfileId = "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0"
    };

    private static XDocument ParseXml(string xml)
    {
        // Strip the XML declaration line so XDocument.Parse works
        var body = xml.Contains("<?xml") ? xml[(xml.IndexOf("?>") + 2)..].TrimStart() : xml;
        return XDocument.Parse(body);
    }

    [Fact]
    public async Task TransformToUblXmlAsync_WithValidInvoice_ReturnsValidXml()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice();

        var xml = await _sut.TransformToUblXmlAsync(invoice, DefaultFormatVersion);

        xml.Should().NotBeNullOrEmpty();
        xml.Should().Contain("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2");
    }

    [Fact]
    public async Task TransformToUblXmlAsync_IntraCommunityInvoice_EmitsDeliveryElement()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice();
        invoice.DeliveryDate = new DateOnly(2026, 3, 8);
        invoice.DeliveryCountryCode = "DE";

        var xml = await _sut.TransformToUblXmlAsync(invoice, DefaultFormatVersion);
        var doc = ParseXml(xml);

        var delivery = doc.Root!.Element(Cac + "Delivery");
        delivery.Should().NotBeNull();
        delivery!.Element(Cbc + "ActualDeliveryDate")!.Value.Should().Be("2026-03-08");
        delivery.Element(Cac + "DeliveryLocation")!
            .Element(Cac + "Address")!
            .Element(Cac + "Country")!
            .Element(Cbc + "IdentificationCode")!.Value.Should().Be("DE");
    }

    [Fact]
    public async Task TransformToUblXmlAsync_IntraCommunityInvoice_FallsBackToBuyerCountry()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice();
        invoice.DeliveryDate = null;
        invoice.DeliveryCountryCode = null;
        // Tax category K triggers fallback

        var xml = await _sut.TransformToUblXmlAsync(invoice, DefaultFormatVersion);
        var doc = ParseXml(xml);

        var delivery = doc.Root!.Element(Cac + "Delivery");
        delivery.Should().NotBeNull("BR-IC-11/12 require Delivery for category K");
        delivery!.Element(Cbc + "ActualDeliveryDate")!.Value
            .Should().Be(invoice.IssueDate.ToString("yyyy-MM-dd"), "should fall back to IssueDate");
        delivery.Element(Cac + "DeliveryLocation")!
            .Element(Cac + "Address")!
            .Element(Cac + "Country")!
            .Element(Cbc + "IdentificationCode")!.Value
            .Should().Be("DE", "should fall back to buyer country");
    }

    [Fact]
    public async Task TransformToUblXmlAsync_NoteAppearsBeforeDocumentCurrencyCode()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice();

        var xml = await _sut.TransformToUblXmlAsync(invoice, DefaultFormatVersion);
        var doc = ParseXml(xml);
        var elements = doc.Root!.Elements().Select(e => e.Name.LocalName).ToList();

        var noteIdx = elements.IndexOf("Note");
        var currencyIdx = elements.IndexOf("DocumentCurrencyCode");
        noteIdx.Should().BeLessThan(currencyIdx, "Note (BT-22) must precede DocumentCurrencyCode per UBL 2.1 schema");
    }

    [Fact]
    public async Task TransformToUblXmlAsync_TelephoneAppearsBeforeElectronicMail()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice();
        invoice.Seller.Contact = new Contact { Name = "Test", Phone = "+32123", Email = "test@x.com" };

        var xml = await _sut.TransformToUblXmlAsync(invoice, DefaultFormatVersion);

        var phoneIdx = xml.IndexOf("Telephone");
        var emailIdx = xml.IndexOf("ElectronicMail");
        phoneIdx.Should().BeLessThan(emailIdx, "Telephone must precede ElectronicMail per UBL 2.1 schema");
    }

    [Fact]
    public async Task TransformToUblXmlAsync_AllowanceChargeAppearsBeforeItem()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice();

        var xml = await _sut.TransformToUblXmlAsync(invoice, DefaultFormatVersion);

        // Line 1 has a discount (AllowanceCharge)
        var allowanceIdx = xml.IndexOf("AllowanceCharge");
        var itemIdx = xml.IndexOf("<cac:Item>");
        allowanceIdx.Should().BeLessThan(itemIdx, "AllowanceCharge must precede Item per UBL 2.1 schema");
    }

    [Fact]
    public async Task TransformToUblXmlAsync_AllowanceChargeReasonAppearsBeforeAmount()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice();

        var xml = await _sut.TransformToUblXmlAsync(invoice, DefaultFormatVersion);

        var reasonIdx = xml.IndexOf("AllowanceChargeReason");
        var amountIdx = xml.IndexOf("<cbc:Amount");
        reasonIdx.Should().BeLessThan(amountIdx, "AllowanceChargeReason must precede Amount per UBL 2.1 schema");
    }
}
