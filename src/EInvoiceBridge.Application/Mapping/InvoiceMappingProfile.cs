using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Application.Mapping;

public static class InvoiceMappingProfile
{
    public static Invoice ToModel(this CreateInvoiceRequest request)
    {
        return new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = request.InvoiceNumber,
            IssueDate = DateOnly.Parse(request.IssueDate),
            DueDate = DateOnly.Parse(request.DueDate),
            InvoiceTypeCode = request.InvoiceTypeCode,
            CurrencyCode = request.CurrencyCode,
            BuyerReference = request.BuyerReference,
            Seller = request.Seller.ToModel(),
            Buyer = request.Buyer.ToModel(),
            PaymentMeans = new PaymentMeans
            {
                Code = request.PaymentMeans.Code,
                Iban = request.PaymentMeans.Iban,
                Bic = request.PaymentMeans.Bic
            },
            Lines = request.Lines.Select(l => new InvoiceLine
            {
                LineNumber = l.LineNumber,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitCode = l.UnitCode,
                UnitPrice = l.UnitPrice,
                Discount = l.Discount is not null ? new Discount { Amount = l.Discount.Amount, Reason = l.Discount.Reason } : null,
                TaxCategoryCode = l.TaxCategoryCode,
                TaxPercent = l.TaxPercent
            }).ToList(),
            TaxExemptionReason = request.TaxExemptionReason,
            Notes = request.Notes
        };
    }

    private static Party ToModel(this PartyDto dto)
    {
        return new Party
        {
            Name = dto.Name,
            VatNumber = dto.VatNumber,
            Address = new Address
            {
                Street = dto.Address.Street,
                City = dto.Address.City,
                PostalCode = dto.Address.PostalCode,
                CountryCode = dto.Address.CountryCode
            },
            Contact = dto.Contact is not null ? new Contact { Name = dto.Contact.Name, Email = dto.Contact.Email, Phone = dto.Contact.Phone } : null
        };
    }
}
