INSERT INTO invoice_audit_entries (id, invoice_id, status, message, details, created_at)
VALUES (@Id, @InvoiceId, @Status, @Message, @Details::jsonb, @CreatedAt);
