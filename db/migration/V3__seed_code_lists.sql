-- V3: Seed reference data

-- EU countries
INSERT INTO countries (code, name, is_eu) VALUES
  ('AT', 'Austria', TRUE),
  ('BE', 'Belgium', TRUE),
  ('BG', 'Bulgaria', TRUE),
  ('CY', 'Cyprus', TRUE),
  ('CZ', 'Czech Republic', TRUE),
  ('DE', 'Germany', TRUE),
  ('DK', 'Denmark', TRUE),
  ('EE', 'Estonia', TRUE),
  ('ES', 'Spain', TRUE),
  ('FI', 'Finland', TRUE),
  ('FR', 'France', TRUE),
  ('GR', 'Greece', TRUE),
  ('HR', 'Croatia', TRUE),
  ('HU', 'Hungary', TRUE),
  ('IE', 'Ireland', TRUE),
  ('IT', 'Italy', TRUE),
  ('LT', 'Lithuania', TRUE),
  ('LU', 'Luxembourg', TRUE),
  ('LV', 'Latvia', TRUE),
  ('MT', 'Malta', TRUE),
  ('NL', 'Netherlands', TRUE),
  ('PL', 'Poland', TRUE),
  ('PT', 'Portugal', TRUE),
  ('RO', 'Romania', TRUE),
  ('SE', 'Sweden', TRUE),
  ('SI', 'Slovenia', TRUE),
  ('SK', 'Slovakia', TRUE);

-- Format definitions
INSERT INTO format_definitions (id, name, description) VALUES
  ('a0000000-0000-0000-0000-000000000001', 'XRechnung', 'German e-invoicing standard based on EN 16931'),
  ('a0000000-0000-0000-0000-000000000002', 'Peppol BIS', 'Peppol Business Interoperability Specification for Billing');

-- XRechnung 3.0 format version
INSERT INTO format_versions (id, format_definition_id, version, country_code, customization_id, profile_id, status, effective_from, schema_path) VALUES
  ('b0000000-0000-0000-0000-000000000001',
   'a0000000-0000-0000-0000-000000000001',
   '3.0',
   'DE',
   'urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0',
   'urn:fdc:peppol.eu:2017:poacc:billing:01:1.0',
   'active',
   '2024-02-01',
   'Schemas/UBL-2.1');

-- Validation rules for XRechnung 3.0
INSERT INTO format_rules (format_version_id, rule_type, rule_key, priority) VALUES
  ('b0000000-0000-0000-0000-000000000001', 'validation', 'SchemaCompleteness', 10),
  ('b0000000-0000-0000-0000-000000000001', 'validation', 'ArithmeticCheck', 20),
  ('b0000000-0000-0000-0000-000000000001', 'validation', 'VatLogic', 30),
  ('b0000000-0000-0000-0000-000000000001', 'validation', 'IdentifierFormat', 40),
  ('b0000000-0000-0000-0000-000000000001', 'validation', 'GermanBusinessRules', 50);

-- Universal code lists (not tied to a format version)

-- Tax category codes
INSERT INTO code_lists (id, list_type, version) VALUES
  ('c0000000-0000-0000-0000-000000000001', 'tax_category', 'UNCL5305');

INSERT INTO code_list_entries (code_list_id, code, name, description) VALUES
  ('c0000000-0000-0000-0000-000000000001', 'S', 'Standard rate', 'Standard VAT rate applies'),
  ('c0000000-0000-0000-0000-000000000001', 'Z', 'Zero rated', 'Zero rated goods'),
  ('c0000000-0000-0000-0000-000000000001', 'E', 'Exempt', 'Exempt from VAT'),
  ('c0000000-0000-0000-0000-000000000001', 'AE', 'Reverse charge', 'VAT reverse charge'),
  ('c0000000-0000-0000-0000-000000000001', 'K', 'Intra-community supply', 'Intra-community supply of goods'),
  ('c0000000-0000-0000-0000-000000000001', 'G', 'Export', 'Export outside the EU'),
  ('c0000000-0000-0000-0000-000000000001', 'O', 'Not subject', 'Services outside scope of tax'),
  ('c0000000-0000-0000-0000-000000000001', 'L', 'Canary Islands', 'Canary Islands general indirect tax'),
  ('c0000000-0000-0000-0000-000000000001', 'M', 'Ceuta and Melilla', 'Tax for production, services and importation in Ceuta and Melilla');

-- Currency codes
INSERT INTO code_lists (id, list_type, version) VALUES
  ('c0000000-0000-0000-0000-000000000002', 'currency', 'ISO4217');

INSERT INTO code_list_entries (code_list_id, code, name) VALUES
  ('c0000000-0000-0000-0000-000000000002', 'EUR', 'Euro'),
  ('c0000000-0000-0000-0000-000000000002', 'USD', 'US Dollar'),
  ('c0000000-0000-0000-0000-000000000002', 'GBP', 'Pound Sterling'),
  ('c0000000-0000-0000-0000-000000000002', 'CHF', 'Swiss Franc'),
  ('c0000000-0000-0000-0000-000000000002', 'SEK', 'Swedish Krona'),
  ('c0000000-0000-0000-0000-000000000002', 'DKK', 'Danish Krone'),
  ('c0000000-0000-0000-0000-000000000002', 'NOK', 'Norwegian Krone'),
  ('c0000000-0000-0000-0000-000000000002', 'PLN', 'Polish Zloty'),
  ('c0000000-0000-0000-0000-000000000002', 'CZK', 'Czech Koruna'),
  ('c0000000-0000-0000-0000-000000000002', 'HUF', 'Hungarian Forint'),
  ('c0000000-0000-0000-0000-000000000002', 'RON', 'Romanian Leu'),
  ('c0000000-0000-0000-0000-000000000002', 'BGN', 'Bulgarian Lev');

-- Unit codes (common subset)
INSERT INTO code_lists (id, list_type, version) VALUES
  ('c0000000-0000-0000-0000-000000000003', 'unit', 'UNECERec20');

INSERT INTO code_list_entries (code_list_id, code, name) VALUES
  ('c0000000-0000-0000-0000-000000000003', 'C62', 'One (unit/piece)'),
  ('c0000000-0000-0000-0000-000000000003', 'DAY', 'Day'),
  ('c0000000-0000-0000-0000-000000000003', 'HUR', 'Hour'),
  ('c0000000-0000-0000-0000-000000000003', 'KGM', 'Kilogram'),
  ('c0000000-0000-0000-0000-000000000003', 'KTM', 'Kilometre'),
  ('c0000000-0000-0000-0000-000000000003', 'LTR', 'Litre'),
  ('c0000000-0000-0000-0000-000000000003', 'MTR', 'Metre'),
  ('c0000000-0000-0000-0000-000000000003', 'MTK', 'Square metre'),
  ('c0000000-0000-0000-0000-000000000003', 'MTQ', 'Cubic metre'),
  ('c0000000-0000-0000-0000-000000000003', 'TNE', 'Tonne'),
  ('c0000000-0000-0000-0000-000000000003', 'SET', 'Set'),
  ('c0000000-0000-0000-0000-000000000003', 'XPK', 'Package');
