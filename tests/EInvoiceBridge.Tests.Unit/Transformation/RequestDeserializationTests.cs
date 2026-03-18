using System.Text.Json;
using EInvoiceBridge.Core.DTOs;
using FluentAssertions;

namespace EInvoiceBridge.Tests.Unit.Transformation;

public class RequestDeserializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static CreateInvoiceRequest LoadRequest()
    {
        var json = File.ReadAllText(Path.Combine(FindRepoRoot(), "request.json"));
        return JsonSerializer.Deserialize<CreateInvoiceRequest>(json, JsonOptions)!;
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "request.json")))
            dir = Directory.GetParent(dir)?.FullName;
        return dir ?? throw new InvalidOperationException("Could not find repo root with request.json");
    }

    [Fact]
    public void Deserialize_TopLevelFields_ArePopulated()
    {
        var req = LoadRequest();

        req.InvoiceNumber.Should().Be("INV-2026-0847");
        req.IssueDate.Should().Be("2026-03-08");
        req.DueDate.Should().Be("2026-04-07");
        req.InvoiceTypeCode.Should().Be("380");
        req.CurrencyCode.Should().Be("EUR");
        req.BuyerReference.Should().Be("PO-2026-1234");
        req.DeliveryDate.Should().Be("2026-03-08");
        req.DeliveryCountryCode.Should().Be("DE");
        req.DeliveryCity.Should().Be("Stuttgart");
        req.DeliveryPostalCode.Should().Be("70173");
        req.Notes.Should().Be("Delivery ref: DEL-2026-0412");
        req.TaxExemptionReason.Should().Be("Intra-community supply — Article 138 Council Directive 2006/112/EC");
    }

    [Fact]
    public void Deserialize_Seller_IsFullyPopulated()
    {
        var req = LoadRequest();
        var seller = req.Seller;

        seller.Name.Should().Be("Van Houten Industrial BV");
        seller.VatNumber.Should().Be("BE0123456789");
        seller.Address.Street.Should().Be("Industrielaan 42");
        seller.Address.City.Should().Be("Ghent");
        seller.Address.PostalCode.Should().Be("9000");
        seller.Address.CountryCode.Should().Be("BE");
        seller.Contact.Should().NotBeNull();
        seller.Contact!.Name.Should().Be("Ingrid Peeters");
        seller.Contact.Email.Should().Be("ingrid@vanhouten.be");
        seller.Contact.Phone.Should().Be("+32 9 123 45 67");
    }

    [Fact]
    public void Deserialize_Buyer_IsFullyPopulated()
    {
        var req = LoadRequest();
        var buyer = req.Buyer;

        buyer.Name.Should().Be("Müller GmbH");
        buyer.VatNumber.Should().Be("DE123456789");
        buyer.Address.Street.Should().Be("Hauptstraße 15");
        buyer.Address.City.Should().Be("Stuttgart");
        buyer.Address.PostalCode.Should().Be("70173");
        buyer.Address.CountryCode.Should().Be("DE");
        buyer.Contact.Should().BeNull();
    }

    [Fact]
    public void Deserialize_PaymentMeans_IsFullyPopulated()
    {
        var req = LoadRequest();
        var pm = req.PaymentMeans;

        pm.Code.Should().Be("30");
        pm.Iban.Should().Be("BE68539007547034");
        pm.Bic.Should().Be("BBRUBEBB");
    }

    [Fact]
    public void Deserialize_Lines_AreFullyPopulated()
    {
        var req = LoadRequest();

        req.Lines.Should().HaveCount(2);

        var line1 = req.Lines[0];
        line1.LineNumber.Should().Be(1);
        line1.Description.Should().Be("Hydraulic fitting HF-2240");
        line1.Quantity.Should().Be(500);
        line1.UnitCode.Should().Be("C62");
        line1.UnitPrice.Should().Be(22.50m);
        line1.Discount.Should().NotBeNull();
        line1.Discount!.Amount.Should().Be(562.50m);
        line1.Discount.Reason.Should().Be("Volume discount 5%");
        line1.TaxCategoryCode.Should().Be("K");
        line1.TaxPercent.Should().Be(0);

        var line2 = req.Lines[1];
        line2.LineNumber.Should().Be(2);
        line2.Description.Should().Be("Hydraulic fitting HF-3100");
        line2.Quantity.Should().Be(100);
        line2.UnitCode.Should().Be("C62");
        line2.UnitPrice.Should().Be(13.50m);
        line2.Discount.Should().NotBeNull();
        line2.Discount!.Amount.Should().Be(67.50m);
        line2.Discount.Reason.Should().Be("Volume discount 5%");
        line2.TaxCategoryCode.Should().Be("K");
        line2.TaxPercent.Should().Be(0);
    }
}
