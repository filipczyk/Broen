SELECT fr.id, fr.format_version_id, fr.rule_type, fr.rule_key,
       fr.rule_config, fr.priority, fr.is_enabled
FROM format_rules fr
WHERE fr.format_version_id = @FormatVersionId
  AND fr.rule_type = @RuleType
  AND fr.is_enabled = TRUE
ORDER BY fr.priority;
