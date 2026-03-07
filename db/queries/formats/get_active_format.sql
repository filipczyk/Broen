SELECT fv.id, fv.format_definition_id, fv.version, fv.country_code,
       fv.customization_id, fv.profile_id, fv.status,
       fv.effective_from, fv.effective_until, fv.schema_path,
       fd.name AS format_name
FROM format_versions fv
JOIN format_definitions fd ON fd.id = fv.format_definition_id
WHERE fv.country_code = @CountryCode
  AND fv.status = 'active';
