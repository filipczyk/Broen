INSERT INTO invoices (id, invoice_number, status, format_version_id, raw_json, created_at, updated_at)
VALUES (@Id, @InvoiceNumber, @Status, @FormatVersionId, @RawJson::jsonb, @CreatedAt, @UpdatedAt)
RETURNING id;
