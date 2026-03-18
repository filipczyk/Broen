using System.Xml.Linq;
using EInvoiceBridge.Core.Models;
using EInvoiceBridge.Transformation;
using FluentAssertions;

namespace EInvoiceBridge.Tests.Unit.Transformation;

public class UblXmlGoldenOutputTests
{
    private static readonly XNamespace Cac = XmlNamespaces.Cac;
    private static readonly XNamespace Cbc = XmlNamespaces.Cbc;

    private static readonly FormatVersion DefaultFormatVersion = new()
    {
        Id = Guid.NewGuid(),
        CustomizationId = "urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0",
        ProfileId = "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0"
    };

    private readonly UblInvoiceTransformer _sut = new();

    private async Task<XDocument> TransformAndParse()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice();
        var xml = await _sut.TransformToUblXmlAsync(invoice, DefaultFormatVersion);
        var body = xml.Contains("<?xml") ? xml[(xml.IndexOf("?>") + 2)..].TrimStart() : xml;
        return XDocument.Parse(body);
    }

    [Fact]
    public async Task Header_AllFieldsPresent()
    {
        var doc = await TransformAndParse();
        var root = doc.Root!;

        root.Element(Cbc + "CustomizationID")!.Value
            .Should().Be("urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0");
        root.Element(Cbc + "ProfileID")!.Value
            .Should().Be("urn:fdc:peppol.eu:2017:poacc:billing:01:1.0");
        root.Element(Cbc + "ID")!.Value.Should().Be("INV-2026-0847");
        root.Element(Cbc + "IssueDate")!.Value.Should().Be("2026-03-06");
        root.Element(Cbc + "DueDate")!.Value.Should().Be("2026-04-05");
        root.Element(Cbc + "InvoiceTypeCode")!.Value.Should().Be("380");
        root.Element(Cbc + "Note")!.Value.Should().Be("Delivery ref: DEL-2026-0412");
        root.Element(Cbc + "DocumentCurrencyCode")!.Value.Should().Be("EUR");
        root.Element(Cbc + "BuyerReference")!.Value.Should().Be("PO-2026-1234");
    }

    [Fact]
    public async Task SupplierParty_AllFieldsPresent()
    {
        var doc = await TransformAndParse();
        var party = doc.Root!.Element(Cac + "AccountingSupplierParty")!.Element(Cac + "Party")!;

        var endpoint = party.Element(Cbc + "EndpointID")!;
        endpoint.Attribute("schemeID")!.Value.Should().Be("0208");
        endpoint.Value.Should().Be("BE0123456789");

        party.Element(Cac + "PartyName")!.Element(Cbc + "Name")!.Value
            .Should().Be("Van Houten Industrial BV");

        var address = party.Element(Cac + "PostalAddress")!;
        address.Element(Cbc + "StreetName")!.Value.Should().Be("Industrielaan 42");
        address.Element(Cbc + "CityName")!.Value.Should().Be("Ghent");
        address.Element(Cbc + "PostalZone")!.Value.Should().Be("9000");
        address.Element(Cac + "Country")!.Element(Cbc + "IdentificationCode")!.Value.Should().Be("BE");

        party.Element(Cac + "PartyTaxScheme")!.Element(Cbc + "CompanyID")!.Value
            .Should().Be("BE0123456789");
        party.Element(Cac + "PartyLegalEntity")!.Element(Cbc + "RegistrationName")!.Value
            .Should().Be("Van Houten Industrial BV");

        var contact = party.Element(Cac + "Contact")!;
        contact.Element(Cbc + "Name")!.Value.Should().Be("Ingrid Peeters");
        contact.Element(Cbc + "ElectronicMail")!.Value.Should().Be("ingrid@vanhouten.be");
    }

    [Fact]
    public async Task CustomerParty_AllFieldsPresent()
    {
        var doc = await TransformAndParse();
        var party = doc.Root!.Element(Cac + "AccountingCustomerParty")!.Element(Cac + "Party")!;

        var endpoint = party.Element(Cbc + "EndpointID")!;
        endpoint.Attribute("schemeID")!.Value.Should().Be("9930");
        endpoint.Value.Should().Be("DE123456789");

        party.Element(Cac + "PartyName")!.Element(Cbc + "Name")!.Value
            .Should().Be("Müller GmbH");

        var address = party.Element(Cac + "PostalAddress")!;
        address.Element(Cbc + "StreetName")!.Value.Should().Be("Hauptstraße 15");
        address.Element(Cbc + "CityName")!.Value.Should().Be("Stuttgart");
        address.Element(Cbc + "PostalZone")!.Value.Should().Be("70173");
        address.Element(Cac + "Country")!.Element(Cbc + "IdentificationCode")!.Value.Should().Be("DE");

        party.Element(Cac + "PartyTaxScheme")!.Element(Cbc + "CompanyID")!.Value
            .Should().Be("DE123456789");
        party.Element(Cac + "PartyLegalEntity")!.Element(Cbc + "RegistrationName")!.Value
            .Should().Be("Müller GmbH");
    }

    [Fact]
    public async Task Delivery_AllFieldsPresent()
    {
        var doc = await TransformAndParse();
        var delivery = doc.Root!.Element(Cac + "Delivery")!;

        delivery.Element(Cbc + "ActualDeliveryDate")!.Value.Should().Be("2026-03-08");

        var address = delivery.Element(Cac + "DeliveryLocation")!.Element(Cac + "Address")!;
        address.Element(Cbc + "StreetName")!.Value.Should().Be("Hauptstraße 15");
        address.Element(Cbc + "CityName")!.Value.Should().Be("Stuttgart");
        address.Element(Cbc + "PostalZone")!.Value.Should().Be("70173");
        address.Element(Cac + "Country")!.Element(Cbc + "IdentificationCode")!.Value.Should().Be("DE");
    }

    [Fact]
    public async Task PaymentMeans_AllFieldsPresent()
    {
        var doc = await TransformAndParse();
        var pm = doc.Root!.Element(Cac + "PaymentMeans")!;

        pm.Element(Cbc + "PaymentMeansCode")!.Value.Should().Be("30");
        var account = pm.Element(Cac + "PayeeFinancialAccount")!;
        account.Element(Cbc + "ID")!.Value.Should().Be("BE68539007547034");
        account.Element(Cac + "FinancialInstitutionBranch")!.Element(Cbc + "ID")!.Value
            .Should().Be("BBRUBEBB");
    }

    [Fact]
    public async Task TaxTotal_AllFieldsPresent()
    {
        var doc = await TransformAndParse();
        var taxTotal = doc.Root!.Element(Cac + "TaxTotal")!;

        var taxAmount = taxTotal.Element(Cbc + "TaxAmount")!;
        taxAmount.Value.Should().Be("0.00");
        taxAmount.Attribute("currencyID")!.Value.Should().Be("EUR");

        var subtotal = taxTotal.Element(Cac + "TaxSubtotal")!;
        var taxableAmount = subtotal.Element(Cbc + "TaxableAmount")!;
        taxableAmount.Value.Should().Be("11970.00");
        taxableAmount.Attribute("currencyID")!.Value.Should().Be("EUR");

        var subTaxAmount = subtotal.Element(Cbc + "TaxAmount")!;
        subTaxAmount.Value.Should().Be("0.00");
        subTaxAmount.Attribute("currencyID")!.Value.Should().Be("EUR");

        var category = subtotal.Element(Cac + "TaxCategory")!;
        category.Element(Cbc + "ID")!.Value.Should().Be("K");
        category.Element(Cbc + "Percent")!.Value.Should().Be("0.00");
        category.Element(Cbc + "TaxExemptionReason")!.Value
            .Should().Be("Intra-community supply — Article 138 Council Directive 2006/112/EC");
    }

    [Fact]
    public async Task LegalMonetaryTotal_AllFieldsPresent()
    {
        var doc = await TransformAndParse();
        var total = doc.Root!.Element(Cac + "LegalMonetaryTotal")!;

        AssertAmount(total, "LineExtensionAmount", "11970.00", "EUR");
        AssertAmount(total, "TaxExclusiveAmount", "11970.00", "EUR");
        AssertAmount(total, "TaxInclusiveAmount", "11970.00", "EUR");
        AssertAmount(total, "PayableAmount", "11970.00", "EUR");
    }

    [Fact]
    public async Task InvoiceLines_AllFieldsPresent()
    {
        var doc = await TransformAndParse();
        var lines = doc.Root!.Elements(Cac + "InvoiceLine").ToList();
        lines.Should().HaveCount(2);

        // Line 1: 500 × 22.50 - 562.50 = 10687.50
        var line1 = lines[0];
        line1.Element(Cbc + "ID")!.Value.Should().Be("1");
        var qty1 = line1.Element(Cbc + "InvoicedQuantity")!;
        qty1.Value.Should().Be("500.00");
        qty1.Attribute("unitCode")!.Value.Should().Be("C62");
        AssertAmount(line1, "LineExtensionAmount", "10687.50", "EUR");

        var ac1 = line1.Element(Cac + "AllowanceCharge")!;
        ac1.Element(Cbc + "ChargeIndicator")!.Value.Should().Be("false");
        ac1.Element(Cbc + "AllowanceChargeReason")!.Value.Should().Be("Volume discount 5%");
        var acAmount1 = ac1.Element(Cbc + "Amount")!;
        acAmount1.Value.Should().Be("562.50");
        acAmount1.Attribute("currencyID")!.Value.Should().Be("EUR");

        var item1 = line1.Element(Cac + "Item")!;
        item1.Element(Cbc + "Description")!.Value.Should().Be("Hydraulic fitting HF-2240");
        item1.Element(Cbc + "Name")!.Value.Should().Be("Hydraulic fitting HF-2240");
        var taxCat1 = item1.Element(Cac + "ClassifiedTaxCategory")!;
        taxCat1.Element(Cbc + "ID")!.Value.Should().Be("K");
        taxCat1.Element(Cbc + "Percent")!.Value.Should().Be("0.00");

        var price1 = line1.Element(Cac + "Price")!.Element(Cbc + "PriceAmount")!;
        price1.Value.Should().Be("22.50");
        price1.Attribute("currencyID")!.Value.Should().Be("EUR");

        // Line 2: 100 × 13.50 - 67.50 = 1282.50
        var line2 = lines[1];
        line2.Element(Cbc + "ID")!.Value.Should().Be("2");
        var qty2 = line2.Element(Cbc + "InvoicedQuantity")!;
        qty2.Value.Should().Be("100.00");
        qty2.Attribute("unitCode")!.Value.Should().Be("C62");
        AssertAmount(line2, "LineExtensionAmount", "1282.50", "EUR");

        var ac2 = line2.Element(Cac + "AllowanceCharge")!;
        ac2.Element(Cbc + "ChargeIndicator")!.Value.Should().Be("false");
        ac2.Element(Cbc + "AllowanceChargeReason")!.Value.Should().Be("Volume discount 5%");

        var item2 = line2.Element(Cac + "Item")!;
        item2.Element(Cbc + "Description")!.Value.Should().Be("Hydraulic fitting HF-3100");
        item2.Element(Cbc + "Name")!.Value.Should().Be("Hydraulic fitting HF-3100");
    }

    private static void AssertAmount(XElement parent, string elementName, string expectedValue, string expectedCurrency)
    {
        var element = parent.Element(Cbc + elementName)!;
        element.Value.Should().Be(expectedValue);
        element.Attribute("currencyID")!.Value.Should().Be(expectedCurrency);
    }
}
