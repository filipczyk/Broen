SELECT cle.code, cle.name, cle.description, cle.is_active
FROM code_list_entries cle
JOIN code_lists cl ON cl.id = cle.code_list_id
WHERE cl.list_type = @ListType
  AND (cl.format_version_id = @FormatVersionId OR cl.format_version_id IS NULL)
  AND cle.is_active = TRUE
ORDER BY cle.code;
