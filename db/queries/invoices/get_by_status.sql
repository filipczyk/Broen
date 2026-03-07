SELECT i.id, i.invoice_number, i.status, i.format_version_id,
       i.raw_json, i.validation_result, i.generated_xml,
       i.storecove_submission_id, i.created_at, i.updated_at
FROM invoices i
WHERE i.status = @Status
ORDER BY i.created_at DESC
LIMIT @Limit OFFSET @Offset;
