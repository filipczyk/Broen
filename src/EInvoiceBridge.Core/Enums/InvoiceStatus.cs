namespace EInvoiceBridge.Core.Enums;

public enum InvoiceStatus
{
    Received,
    Validating,
    Valid,
    Invalid,
    Transforming,
    Sending,
    Sent,
    Delivered,
    Failed
}
