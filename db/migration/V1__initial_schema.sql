-- V1: Core invoice tables

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE invoices (
  id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  invoice_number          VARCHAR(100) NOT NULL,
  status                  VARCHAR(50) NOT NULL DEFAULT 'Received',
  format_version_id       UUID,
  raw_json                JSONB NOT NULL,
  validation_result       JSONB,
  generated_xml           TEXT,
  storecove_submission_id VARCHAR(255),
  created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE invoice_lines (
  id                UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  invoice_id        UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
  line_number       INT NOT NULL,
  description       VARCHAR(500) NOT NULL,
  quantity          NUMERIC(18,4) NOT NULL,
  unit_code         VARCHAR(10) NOT NULL,
  unit_price        NUMERIC(18,4) NOT NULL,
  discount_amount   NUMERIC(18,2),
  discount_reason   VARCHAR(500),
  tax_category_code VARCHAR(10) NOT NULL,
  tax_percent       NUMERIC(5,2) NOT NULL DEFAULT 0,
  line_net_amount   NUMERIC(18,2) NOT NULL,
  created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE invoice_audit_entries (
  id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  invoice_id  UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
  status      VARCHAR(50) NOT NULL,
  message     TEXT,
  details     JSONB,
  created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_invoices_invoice_number ON invoices (invoice_number);
CREATE INDEX idx_invoices_status ON invoices (status);
CREATE INDEX idx_invoices_created_at ON invoices (created_at DESC);
CREATE INDEX idx_invoices_storecove_id ON invoices (storecove_submission_id)
  WHERE storecove_submission_id IS NOT NULL;

CREATE INDEX idx_invoice_lines_invoice_id ON invoice_lines (invoice_id);
CREATE INDEX idx_invoice_audit_entries_invoice_id ON invoice_audit_entries (invoice_id);
CREATE INDEX idx_invoice_audit_entries_created_at ON invoice_audit_entries (invoice_id, created_at DESC);
