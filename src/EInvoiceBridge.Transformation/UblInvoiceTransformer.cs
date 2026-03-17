using System.Xml.Linq;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Transformation;

public sealed class UblInvoiceTransformer : ITransformationService
{
    private static readonly XNamespace Ns = XmlNamespaces.Invoice;
    private static readonly XNamespace Cac = XmlNamespaces.Cac;
    private static readonly XNamespace Cbc = XmlNamespaces.Cbc;

    public Task<string> TransformToUblXmlAsync(Invoice invoice, FormatVersion formatVersion, CancellationToken cancellationToken = default)
    {
        var currency = invoice.CurrencyCode;

        // Compute line extensions
        var lineExtensions = invoice.Lines.Select(l =>
        {
            var net = l.Quantity * l.UnitPrice - (l.Discount?.Amount ?? 0m);
            return (Line: l, NetAmount: Math.Round(net, 2));
        }).ToList();

        var lineExtensionTotal = lineExtensions.Sum(x => x.NetAmount);

        // Group tax totals by category+percent
        var taxGroups = lineExtensions
            .GroupBy(x => (x.Line.TaxCategoryCode, x.Line.TaxPercent))
            .Select(g =>
            {
                var taxableAmount = g.Sum(x => x.NetAmount);
                var taxAmount = Math.Round(taxableAmount * g.Key.TaxPercent / 100m, 2);
                return new TaxBreakdown
                {
                    TaxCategoryCode = g.Key.TaxCategoryCode,
                    TaxPercent = g.Key.TaxPercent,
                    TaxableAmount = taxableAmount,
                    TaxAmount = taxAmount,
                    TaxExemptionReason = g.Key.TaxCategoryCode is "K" or "AE" ? invoice.TaxExemptionReason : null
                };
            }).ToList();

        var totalTax = taxGroups.Sum(t => t.TaxAmount);
        var payableAmount = lineExtensionTotal + totalTax;

        var customizationId = formatVersion.CustomizationId
            ?? "urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0";
        var profileId = formatVersion.ProfileId
            ?? "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0";

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(Ns + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cac", Cac.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "cbc", Cbc.NamespaceName),
                new XElement(Cbc + "CustomizationID", customizationId),
                new XElement(Cbc + "ProfileID", profileId),
                new XElement(Cbc + "ID", invoice.InvoiceNumber),
                new XElement(Cbc + "IssueDate", invoice.IssueDate.ToString("yyyy-MM-dd")),
                new XElement(Cbc + "DueDate", invoice.DueDate.ToString("yyyy-MM-dd")),
                new XElement(Cbc + "InvoiceTypeCode", invoice.InvoiceTypeCode),
                invoice.Notes is not null ? new XElement(Cbc + "Note", invoice.Notes) : null!,
                new XElement(Cbc + "DocumentCurrencyCode", currency),
                new XElement(Cbc + "BuyerReference", invoice.BuyerReference),
                BuildPartyElement("AccountingSupplierParty", invoice.Seller),
                BuildPartyElement("AccountingCustomerParty", invoice.Buyer),
                BuildDelivery(invoice),
                BuildPaymentMeans(invoice.PaymentMeans),
                BuildTaxTotal(taxGroups, currency, totalTax),
                BuildLegalMonetaryTotal(lineExtensionTotal, totalTax, payableAmount, currency),
                lineExtensions.Select((le, idx) => BuildInvoiceLine(le.Line, le.NetAmount, currency))
            )
        );

        // Remove null elements (e.g., when Notes is null)
        doc.Descendants().Where(e => e.IsEmpty && !e.HasAttributes && !e.HasElements && string.IsNullOrEmpty(e.Value)).Remove();

        var xml = doc.Declaration + Environment.NewLine + doc.ToString();
        return Task.FromResult(xml);
    }

    private static XElement BuildPartyElement(string role, Party party)
    {
        var schemeId = GetEndpointSchemeId(party.Address.CountryCode);
        return new XElement(Cac + role,
            new XElement(Cac + "Party",
                new XElement(Cbc + "EndpointID",
                    new XAttribute("schemeID", schemeId),
                    party.VatNumber),
                new XElement(Cac + "PartyName",
                    new XElement(Cbc + "Name", party.Name)),
                new XElement(Cac + "PostalAddress",
                    new XElement(Cbc + "StreetName", party.Address.Street),
                    new XElement(Cbc + "CityName", party.Address.City),
                    new XElement(Cbc + "PostalZone", party.Address.PostalCode),
                    new XElement(Cac + "Country",
                        new XElement(Cbc + "IdentificationCode", party.Address.CountryCode))),
                new XElement(Cac + "PartyTaxScheme",
                    new XElement(Cbc + "CompanyID", party.VatNumber),
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID", "VAT"))),
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName", party.Name)),
                party.Contact is not null
                    ? new XElement(Cac + "Contact",
                        party.Contact.Name is not null ? new XElement(Cbc + "Name", party.Contact.Name) : null!,
                        party.Contact.Phone is not null ? new XElement(Cbc + "Telephone", party.Contact.Phone) : null!,
                        party.Contact.Email is not null ? new XElement(Cbc + "ElectronicMail", party.Contact.Email) : null!)
                    : null!
            )
        );
    }

    private static XElement? BuildDelivery(Invoice invoice)
    {
        var hasIntraCommunityCategory = invoice.Lines.Any(l => l.TaxCategoryCode is "K" or "AE");
        var hasExplicitDelivery = invoice.DeliveryDate is not null || invoice.DeliveryCountryCode is not null;

        if (!hasIntraCommunityCategory && !hasExplicitDelivery)
            return null;

        var deliveryDate = invoice.DeliveryDate ?? invoice.IssueDate;
        var countryCode = invoice.DeliveryCountryCode ?? invoice.Buyer.Address.CountryCode;
        var city = invoice.DeliveryCity ?? invoice.Buyer.Address.City;
        var postalCode = invoice.DeliveryPostalCode ?? invoice.Buyer.Address.PostalCode;

        return new XElement(Cac + "Delivery",
            new XElement(Cbc + "ActualDeliveryDate", deliveryDate.ToString("yyyy-MM-dd")),
            new XElement(Cac + "DeliveryLocation",
                new XElement(Cac + "Address",
                    new XElement(Cbc + "StreetName", invoice.Buyer.Address.Street),
                    new XElement(Cbc + "CityName", city),
                    new XElement(Cbc + "PostalZone", postalCode),
                    new XElement(Cac + "Country",
                        new XElement(Cbc + "IdentificationCode", countryCode)))));
    }

    private static XElement BuildPaymentMeans(PaymentMeans pm)
    {
        var element = new XElement(Cac + "PaymentMeans",
            new XElement(Cbc + "PaymentMeansCode", pm.Code),
            new XElement(Cac + "PayeeFinancialAccount",
                new XElement(Cbc + "ID", pm.Iban)));

        if (pm.Bic is not null)
        {
            element.Element(Cac + "PayeeFinancialAccount")!.Add(
                new XElement(Cac + "FinancialInstitutionBranch",
                    new XElement(Cbc + "ID", pm.Bic)));
        }

        return element;
    }

    private static XElement BuildTaxTotal(List<TaxBreakdown> taxGroups, string currency, decimal totalTax)
    {
        return new XElement(Cac + "TaxTotal",
            new XElement(Cbc + "TaxAmount", new XAttribute("currencyID", currency), Fmt(totalTax)),
            taxGroups.Select(t =>
                new XElement(Cac + "TaxSubtotal",
                    new XElement(Cbc + "TaxableAmount", new XAttribute("currencyID", currency), Fmt(t.TaxableAmount)),
                    new XElement(Cbc + "TaxAmount", new XAttribute("currencyID", currency), Fmt(t.TaxAmount)),
                    new XElement(Cac + "TaxCategory",
                        new XElement(Cbc + "ID", t.TaxCategoryCode),
                        new XElement(Cbc + "Percent", Fmt(t.TaxPercent)),
                        t.TaxExemptionReason is not null
                            ? new XElement(Cbc + "TaxExemptionReason", t.TaxExemptionReason)
                            : null!,
                        new XElement(Cac + "TaxScheme",
                            new XElement(Cbc + "ID", "VAT")))
                )
            )
        );
    }

    private static XElement BuildLegalMonetaryTotal(decimal lineExtension, decimal tax, decimal payable, string currency)
    {
        return new XElement(Cac + "LegalMonetaryTotal",
            new XElement(Cbc + "LineExtensionAmount", new XAttribute("currencyID", currency), Fmt(lineExtension)),
            new XElement(Cbc + "TaxExclusiveAmount", new XAttribute("currencyID", currency), Fmt(lineExtension)),
            new XElement(Cbc + "TaxInclusiveAmount", new XAttribute("currencyID", currency), Fmt(payable)),
            new XElement(Cbc + "PayableAmount", new XAttribute("currencyID", currency), Fmt(payable)));
    }

    private static XElement BuildInvoiceLine(InvoiceLine line, decimal netAmount, string currency)
    {
        var lineElement = new XElement(Cac + "InvoiceLine",
            new XElement(Cbc + "ID", line.LineNumber.ToString()),
            new XElement(Cbc + "InvoicedQuantity",
                new XAttribute("unitCode", line.UnitCode),
                Fmt(line.Quantity)),
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", currency),
                Fmt(netAmount)),
            line.Discount is not null
                ? new XElement(Cac + "AllowanceCharge",
                    new XElement(Cbc + "ChargeIndicator", "false"),
                    line.Discount.Reason is not null
                        ? new XElement(Cbc + "AllowanceChargeReason", line.Discount.Reason)
                        : null!,
                    new XElement(Cbc + "Amount",
                        new XAttribute("currencyID", currency),
                        Fmt(line.Discount.Amount)))
                : null!,
            new XElement(Cac + "Item",
                new XElement(Cbc + "Description", line.Description),
                new XElement(Cbc + "Name", line.Description),
                new XElement(Cac + "ClassifiedTaxCategory",
                    new XElement(Cbc + "ID", line.TaxCategoryCode),
                    new XElement(Cbc + "Percent", Fmt(line.TaxPercent)),
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID", "VAT")))),
            new XElement(Cac + "Price",
                new XElement(Cbc + "PriceAmount",
                    new XAttribute("currencyID", currency),
                    Fmt(line.UnitPrice))));

        return lineElement;
    }

    private static string GetEndpointSchemeId(string countryCode) => countryCode.ToUpperInvariant() switch
    {
        "BE" => "0208",
        "DE" => "9930",
        _ => "9930"
    };

    private static string Fmt(decimal value) => value.ToString("F2");
}
