using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Core.DTOs;

public sealed class CreateInvoiceRequest
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string IssueDate { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public string InvoiceTypeCode { get; set; } = "380";
    public string CurrencyCode { get; set; } = "EUR";
    public string BuyerReference { get; set; } = string.Empty;
    public PartyDto Seller { get; set; } = new();
    public PartyDto Buyer { get; set; } = new();
    public PaymentMeansDto PaymentMeans { get; set; } = new();
    public List<InvoiceLineDto> Lines { get; set; } = [];
    public string? TaxExemptionReason { get; set; }
    public string? Notes { get; set; }
    public string? DeliveryDate { get; set; }
    public string? DeliveryCountryCode { get; set; }
}

public sealed class PartyDto
{
    public string Name { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public AddressDto Address { get; set; } = new();
    public ContactDto? Contact { get; set; }
}

public sealed class AddressDto
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
}

public sealed class ContactDto
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public sealed class PaymentMeansDto
{
    public string Code { get; set; } = "30";
    public string Iban { get; set; } = string.Empty;
    public string? Bic { get; set; }
}

public sealed class InvoiceLineDto
{
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitCode { get; set; } = "C62";
    public decimal UnitPrice { get; set; }
    public DiscountDto? Discount { get; set; }
    public string TaxCategoryCode { get; set; } = string.Empty;
    public decimal TaxPercent { get; set; }
}

public sealed class DiscountDto
{
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}
