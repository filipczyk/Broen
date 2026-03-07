-- V2: Format versioning tables

CREATE TABLE countries (
  code        VARCHAR(2) PRIMARY KEY,
  name        VARCHAR(100) NOT NULL,
  is_eu       BOOLEAN NOT NULL DEFAULT FALSE,
  created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE format_definitions (
  id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  name        VARCHAR(100) NOT NULL UNIQUE,
  description VARCHAR(500),
  created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE format_versions (
  id                    UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  format_definition_id  UUID NOT NULL REFERENCES format_definitions(id),
  version               VARCHAR(50) NOT NULL,
  country_code          VARCHAR(2) NOT NULL REFERENCES countries(code),
  customization_id      VARCHAR(500),
  profile_id            VARCHAR(500),
  status                VARCHAR(20) NOT NULL DEFAULT 'draft',
  effective_from        DATE,
  effective_until       DATE,
  schema_path           VARCHAR(500),
  created_at            TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE format_rules (
  id                UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  format_version_id UUID NOT NULL REFERENCES format_versions(id),
  rule_type         VARCHAR(50) NOT NULL,
  rule_key          VARCHAR(100) NOT NULL,
  rule_config       JSONB,
  priority          INT NOT NULL DEFAULT 0,
  is_enabled        BOOLEAN NOT NULL DEFAULT TRUE,
  created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE code_lists (
  id                UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  format_version_id UUID REFERENCES format_versions(id),
  list_type         VARCHAR(50) NOT NULL,
  version           VARCHAR(50),
  created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE code_list_entries (
  id            UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  code_list_id  UUID NOT NULL REFERENCES code_lists(id) ON DELETE CASCADE,
  code          VARCHAR(50) NOT NULL,
  name          VARCHAR(200) NOT NULL,
  description   VARCHAR(500),
  is_active     BOOLEAN NOT NULL DEFAULT TRUE,
  created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Add FK from invoices to format_versions
ALTER TABLE invoices
  ADD CONSTRAINT fk_invoices_format_version
  FOREIGN KEY (format_version_id) REFERENCES format_versions(id);

-- Indexes
CREATE UNIQUE INDEX idx_format_versions_active
  ON format_versions (format_definition_id, country_code)
  WHERE status = 'active';

CREATE INDEX idx_format_rules_version ON format_rules (format_version_id, rule_type);
CREATE INDEX idx_code_list_entries_lookup ON code_list_entries (code_list_id, code);
CREATE INDEX idx_invoices_format_version ON invoices (format_version_id);
