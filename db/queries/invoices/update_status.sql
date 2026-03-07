UPDATE invoices
SET status = @Status,
    validation_result = COALESCE(@ValidationResult::jsonb, validation_result),
    generated_xml = COALESCE(@GeneratedXml, generated_xml),
    storecove_submission_id = COALESCE(@StorecoveSubmissionId, storecove_submission_id),
    updated_at = @UpdatedAt
WHERE id = @Id;
