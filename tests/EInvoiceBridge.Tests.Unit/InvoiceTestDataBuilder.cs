using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Tests.Unit;

public static class InvoiceTestDataBuilder
{
    public static Invoice CreateValidInvoice()
    {
        return new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-2026-0847",
            IssueDate = new DateOnly(2026, 3, 6),
            DueDate = new DateOnly(2026, 4, 5),
            InvoiceTypeCode = "380",
            CurrencyCode = "EUR",
            BuyerReference = "PO-2026-1234",
            Seller = new Party
            {
                Name = "Van Houten Industrial BV",
                VatNumber = "BE0123456789",
                Address = new Address
                {
                    Street = "Industrielaan 42",
                    City = "Ghent",
                    PostalCode = "9000",
                    CountryCode = "BE"
                },
                Contact = new Contact
                {
                    Name = "Ingrid Peeters",
                    Email = "ingrid@vanhouten.be"
                }
            },
            Buyer = new Party
            {
                Name = "Müller GmbH",
                VatNumber = "DE123456789",
                Address = new Address
                {
                    Street = "Hauptstraße 15",
                    City = "Stuttgart",
                    PostalCode = "70173",
                    CountryCode = "DE"
                }
            },
            PaymentMeans = new PaymentMeans
            {
                Code = "30",
                Iban = "BE68539007547034",
                Bic = "BBRUBEBB"
            },
            Lines =
            [
                new InvoiceLine
                {
                    LineNumber = 1,
                    Description = "Hydraulic fitting HF-2240",
                    Quantity = 500,
                    UnitCode = "C62",
                    UnitPrice = 22.50m,
                    Discount = new Discount { Amount = 562.50m, Reason = "Volume discount 5%" },
                    TaxCategoryCode = "K",
                    TaxPercent = 0
                },
                new InvoiceLine
                {
                    LineNumber = 2,
                    Description = "Hydraulic fitting HF-3100",
                    Quantity = 100,
                    UnitCode = "C62",
                    UnitPrice = 13.50m,
                    Discount = new Discount { Amount = 67.50m, Reason = "Volume discount 5%" },
                    TaxCategoryCode = "K",
                    TaxPercent = 0
                }
            ],
            TaxExemptionReason = "Intra-community supply — Article 138 Council Directive 2006/112/EC",
            Notes = "Delivery ref: DEL-2026-0412",
            DeliveryDate = new DateOnly(2026, 3, 8),
            DeliveryCountryCode = "DE"
        };
    }
}
