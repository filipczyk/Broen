# Country Implementation Guide

This guide walks you through every code change, database migration, test, and configuration step required to add support for a new country's e-invoicing format to Broen.

**Audience:** Developer new to the codebase. Assumes familiarity with C#/.NET 8 but not with e-invoicing standards.

**Prerequisites:**
- Docker Compose running (`docker compose up`) — PostgreSQL, Kafka, Redis available
- `dotnet build` passes
- `dotnet test` passes (all existing tests green)
- Read [`docs/FORMAT_SUPPORT.md`](FORMAT_SUPPORT.md) for the priority country matrix, mandate dates, and current implementation status

---

## Glossary

| Term | Definition |
|------|-----------|
| **EN 16931** | The European standard for electronic invoicing. Defines the semantic data model (business terms like BT-1 "Invoice number", BT-10 "Buyer reference"). All EU e-invoicing formats implement this standard. |
| **CIUS** | Core Invoice Usage Specification — a country-specific restriction/extension of EN 16931. XRechnung is Germany's CIUS; each country has its own. A CIUS can make optional fields mandatory or restrict allowed values, but cannot add new fields outside EN 16931. |
| **UBL 2.1** | Universal Business Language — one of two XML syntaxes for EN 16931 (the other is CII). Broen uses UBL. The XML has namespaces like `urn:oasis:names:specification:ubl:schema:xsd:Invoice-2`. |
| **Peppol BIS Billing 3.0** | The Peppol network's specification for exchanging invoices. Built on top of UBL 2.1 + EN 16931. Most EU countries use this for B2B/B2G invoicing via the Peppol network. |
| **CustomizationID** | A URI in the UBL XML header (`<cbc:CustomizationID>`) that identifies which CIUS the invoice conforms to. Validators use this to decide which rules to apply. Example: `urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0` for XRechnung. |
| **ProfileID** | A URI identifying the business process. For Peppol invoicing, always `urn:fdc:peppol.eu:2017:poacc:billing:01:1.0`. |
| **schemeID** | An identifier on `<cbc:EndpointID>` elements in UBL XML that tells the Peppol network how to look up the buyer. Each country has a different scheme (e.g., `"9930"` = German VAT, `"0208"` = Belgian enterprise number). |
| **Clearance model** | A regulatory model where invoices must be submitted to a government platform *before* being sent to the buyer. Italy (SDI), Poland (KSeF), and Romania (e-Factura) use this. Contrasts with **post-audit model** where invoices are sent directly and audited later. |

---

## Two Implementation Paths

Before writing any code, determine which path your country follows. This is the most important architectural decision.

### Path A: Extend the Existing UBL Transformer (Peppol UBL Countries)

**When to use:** The target country uses UBL 2.1 XML delivered via Peppol, with only minor CIUS differences from the existing German XRechnung implementation.

**Examples:** Belgium (Peppol BIS 3.0), Croatia (UBL 2.1), Greece (myDATA overlay on Peppol BIS), Romania (RO_CIUS on UBL).

**What changes:**
- `CustomizationID` string — already parameterized via `FormatVersion.CustomizationId`
- `ProfileID` string — already parameterized via `FormatVersion.ProfileId`
- Endpoint `schemeID` for Peppol participant identification
- Country-specific mandatory/optional fields and validation rules

**What stays the same:**
- XML structure (UBL 2.1 namespaces, element ordering)
- Tax calculation logic
- Storecove delivery mechanism (Peppol network)
- All shared validation rules (SchemaCompleteness, Arithmetic, VatLogic, IdentifierFormat)

**Concrete example — Belgium:** The existing `UblInvoiceTransformer` already generates valid Peppol BIS 3.0 UBL XML. For Belgium, the only code changes are:
1. New Flyway migration with BE `format_version` row (different CustomizationID)
2. Add `"BE" => "0208"` to `GetEndpointSchemeId()` (already exists in codebase)
3. New `BelgianBusinessRule` validation rule
4. New unit tests

No new transformer class needed — the existing one is parameterized.

### Path B: New Format / New Delivery Mechanism

**When to use:** The target country either uses a non-UBL XML format, or requires a fundamentally different delivery mechanism (government clearance platform instead of Peppol).

**Examples:**
- **Italy (FatturaPA):** Different XML schema (not UBL). Delivered through SDI (Sistema di Interscambio), not Peppol. Requires a native `FatturaPaTransformer`.
- **Poland (KSeF):** Government clearance platform. Invoices must be submitted to KSeF *before* being sent to the buyer. Requires a new `IDeliveryService` implementation.
- **France (Chorus Pro / PDP):** Requires a certified PDP. Storecove may act as PDP — verify against their docs.
- **Spain (VeriFactu):** Real-time reporting to tax authority via certified software.

**What's new in Path B:**
- Potentially a new `ITransformationService` implementation
- Potentially a new `IDeliveryService` implementation
- New configuration classes (e.g., `KsefOptions`)
- A **transformer selection factory** (see [Transformer Selection Pattern](#transformer-selection-pattern))

---

## Step-by-Step: Adding a New Country

The following steps use Belgium (Path A) as the primary example, with callouts for Path B differences.

---

### Step 1: Database — Flyway Migration

**What:** Create a SQL migration that registers the country's format version, validation rules, and any country-specific code lists.

**Why:** The pipeline uses `IFormatRepository.GetActiveFormatAsync(countryCode)` to look up which format version to use for a given buyer country. Without a DB row, the format lookup returns `null` and the invoice won't be processed.

**File to create:** `db/migration/V{N}__add_{xx}_format.sql` (next sequential version number after V3)

The migration inserts rows into these tables (defined in `db/migration/V2__format_versioning_tables.sql`):

#### Tables Reference

**`format_definitions`** — One row per format family (e.g., "XRechnung", "Peppol BIS", "FatturaPA"). Check if the format already exists before inserting. For Belgium, "Peppol BIS" already exists from V3 seed data.

**`format_versions`** — One row per country+version combination. Links a country code to a specific format configuration.
- `country_code`: ISO 3166-1 alpha-2 (e.g., `'BE'`)
- `customization_id`: The CIUS URI for `<cbc:CustomizationID>` in the UBL XML
- `profile_id`: Usually `urn:fdc:peppol.eu:2017:poacc:billing:01:1.0` for all Peppol countries
- `status`: Must be `'active'` — the unique partial index `idx_format_versions_active` enforces one active version per format+country
- `effective_from`/`effective_until`: Date range for format version validity
- `schema_path`: Path to XSD files if external validation is needed

**`format_rules`** — One row per validation rule enabled for this format version.
- `rule_type`: `'validation'`, `'transformation'`, or `'delivery'`
- `rule_key`: Must match the `RuleId` property of an `IValidationRule` implementation
- `rule_config`: Optional JSONB for country-specific parameters
- `priority`: Execution order (10, 20, 30...). Lower = runs first
- `is_enabled`: Boolean toggle

**`code_list_entries`** (optional) — Country-specific tax categories, currency codes, or unit codes not in the existing universal code lists.

#### Example: Belgium

```sql
-- V4__add_be_peppol_bis.sql

-- Peppol BIS format_definition already exists from V3 (id = b0000000-0000-0000-0000-000000000002)
-- Insert a Belgian format_version pointing to it

INSERT INTO format_versions (
    id, format_definition_id, version, country_code,
    customization_id, profile_id, status, effective_from, schema_path, created_at
)
VALUES (
    'b0000000-0000-0000-0000-000000000010'::uuid,
    'b0000000-0000-0000-0000-000000000002'::uuid,  -- FK to existing "Peppol BIS" definition
    '3.0',
    'BE',
    'urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0',
    'urn:fdc:peppol.eu:2017:poacc:billing:01:1.0',
    'active',
    '2026-01-01',
    'Schemas/UBL-2.1',
    NOW()
);

-- Register validation rules for BE format version
INSERT INTO format_rules (id, format_version_id, rule_type, rule_key, priority, is_enabled, created_at)
VALUES
    (gen_random_uuid(), 'b0000000-0000-0000-0000-000000000010', 'validation', 'SchemaCompleteness', 10, TRUE, NOW()),
    (gen_random_uuid(), 'b0000000-0000-0000-0000-000000000010', 'validation', 'ArithmeticCheck',    20, TRUE, NOW()),
    (gen_random_uuid(), 'b0000000-0000-0000-0000-000000000010', 'validation', 'VatLogic',           30, TRUE, NOW()),
    (gen_random_uuid(), 'b0000000-0000-0000-0000-000000000010', 'validation', 'IdentifierFormat',   40, TRUE, NOW()),
    (gen_random_uuid(), 'b0000000-0000-0000-0000-000000000010', 'validation', 'BelgianBusinessRules', 50, TRUE, NOW());
```

> **Path B callout:** For a non-UBL country like Italy, also insert a new `format_definitions` row for "FatturaPA" and use the FatturaPA schema identifier as `customization_id`.

---

### Step 2: Validation — Country-Specific Business Rules

**What:** Create a validation rule class that enforces the country's specific e-invoicing requirements.

**Why:** The shared rules (SchemaCompleteness, Arithmetic, VatLogic, IdentifierFormat) cover the EN 16931 base standard. Each CIUS adds country-specific requirements — for example, XRechnung mandates `BuyerReference` (BT-10), which is not an EN 16931 requirement but Germany-specific.

#### How Validation Rules Work

The `IValidationRule` interface (`src/EInvoiceBridge.Core/Interfaces/IValidationRule.cs`):

```csharp
public interface IValidationRule
{
    string RuleId { get; }
    int Priority { get; }
    Task<IReadOnlyList<ValidationErrorDto>> ValidateAsync(
        Invoice invoice, CancellationToken cancellationToken = default);
}
```

All implementations are registered in DI and injected into `ValidationService` via `IEnumerable<IValidationRule>`. The service sorts by `Priority` and executes sequentially:

```csharp
// src/EInvoiceBridge.Validation/ValidationService.cs
public ValidationService(IEnumerable<IValidationRule> rules)
{
    _rules = rules.OrderBy(r => r.Priority);
}
```

#### Current Limitation: All Rules Run for All Countries

Currently, `ValidationService` runs every registered rule against every invoice regardless of buyer country. Country-specific rules must **self-guard** by checking the buyer's country code. See how `GermanBusinessRule` does this (`src/EInvoiceBridge.Validation/Rules/GermanBusinessRule.cs`):

```csharp
public sealed class GermanBusinessRule : IValidationRule
{
    public string RuleId => "GERMAN_BIZ";
    public int Priority => 50;

    public Task<IReadOnlyList<ValidationErrorDto>> ValidateAsync(
        Invoice invoice, CancellationToken cancellationToken = default)
    {
        if (invoice.Buyer.Address.CountryCode != "DE")
            return Task.FromResult<IReadOnlyList<ValidationErrorDto>>(
                Array.Empty<ValidationErrorDto>());

        // ... German-specific validation (BuyerReference required) ...
    }
}
```

#### Required Prerequisite: Country-Aware Validation

Before adding a second country, `ValidationService` must be updated to filter rules by the format version's enabled rules from the database. This requires:

1. Implementing the currently-stubbed `FormatRepository.GetRulesByFormatAsync()` method (`src/EInvoiceBridge.Persistence/Repositories/FormatRepository.cs`) — the SQL query already exists at `db/queries/formats/get_rules_by_format.sql`
2. Adding `FormatVersion` as a parameter to `ValidationService.ValidateAsync()`
3. Filtering `_rules` to only those whose `RuleId` matches an enabled `rule_key` in the DB

```csharp
// Updated ValidationService signature:
public async Task<ValidationResultDto> ValidateAsync(
    Invoice invoice, FormatVersion formatVersion, CancellationToken cancellationToken = default)
{
    var enabledRules = await _formatRepository.GetRulesByFormatAsync(
        formatVersion.Id, "validation", cancellationToken);
    var enabledRuleKeys = enabledRules.Select(r => r.RuleKey).ToHashSet();

    var activeRules = _rules
        .Where(r => enabledRuleKeys.Contains(r.RuleId))
        .OrderBy(r => r.Priority);

    var allErrors = new List<ValidationErrorDto>();
    foreach (var rule in activeRules)
    {
        var errors = await rule.ValidateAsync(invoice, cancellationToken);
        allErrors.AddRange(errors);
    }
    // ...
}
```

This ensures adding a new country's rules doesn't affect existing countries' validation behavior.

#### Creating a New Rule — Belgium Example

**File to create:** `src/EInvoiceBridge.Validation/Rules/BelgianBusinessRule.cs`

```csharp
using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Validation.Rules;

public sealed class BelgianBusinessRule : IValidationRule
{
    public string RuleId => "BelgianBusinessRules";
    public int Priority => 60;

    public Task<IReadOnlyList<ValidationErrorDto>> ValidateAsync(
        Invoice invoice, CancellationToken cancellationToken = default)
    {
        // Self-guard: only apply to Belgian buyers
        if (invoice.Buyer.Address.CountryCode != "BE")
            return Task.FromResult<IReadOnlyList<ValidationErrorDto>>(
                Array.Empty<ValidationErrorDto>());

        var errors = new List<ValidationErrorDto>();

        // Belgian KBO enterprise number format (10 digits, mod 97 check)
        if (!IsValidKboNumber(invoice.Buyer.VatNumber))
        {
            errors.Add(new ValidationErrorDto
            {
                RuleId = RuleId,
                Severity = ValidationSeverity.Error,
                Field = "Buyer.VatNumber",
                Message = "Belgian buyer VAT number must be a valid KBO enterprise number (BE + 10 digits)"
            });
        }

        // Structured communication reference (+++DDD/DDDD/DDDDD+++) for B2G
        // Validate format if present
        // ... additional Belgian-specific checks ...

        return Task.FromResult<IReadOnlyList<ValidationErrorDto>>(errors);
    }

    private static bool IsValidKboNumber(string? vatNumber)
    {
        if (string.IsNullOrWhiteSpace(vatNumber)) return false;
        var digits = vatNumber.StartsWith("BE", StringComparison.OrdinalIgnoreCase)
            ? vatNumber[2..] : vatNumber;
        return digits.Length == 10 && digits.All(char.IsDigit);
    }
}
```

#### Register in DI

Edit `src/EInvoiceBridge.Validation/DependencyInjection.cs` — add one line:

```csharp
services.AddScoped<IValidationRule, BelgianBusinessRule>();
```

The existing `AddValidation()` call in both `Program.cs` files already pulls in all rules — no further changes needed.

#### Country-Specific Validation Reference

| Country | Key Validations |
|---------|----------------|
| **Belgium** | KBO enterprise number format (10 digits, mod 97), Belgian VAT (BE + 10 digits), structured communication reference (+++DDD/DDDD/DDDDD+++) |
| **France** | SIRET number format (14 digits), Chorus Pro service code |
| **Italy** | Codice Destinatario (7-char recipient code), CIG/CUP codes for public contracts, Partita IVA format |
| **Poland** | NIP tax number format, KSeF session token handling |
| **Romania** | CIF fiscal code format, e-Factura index number |
| **Greece** | AFM tax registration (9 digits), MARK number for myDATA |
| **Croatia** | OIB personal identification number (11 digits, mod 11 check) |
| **Spain** | NIF format validation, VeriFactu reporting fields |

> **Path B callout:** For clearance-model countries, validation may also need to check fields required by the government platform that aren't part of UBL. These fields are stored in a `Dictionary<string, string> CountryExtensions` property on the `Invoice` model, accessed via typed constants (see [Domain Model Extensions](#domain-model-extensions)).

---

### Step 3: Transformation — Country-Specific XML Generation

**What:** Ensure the UBL XML output meets the target country's CIUS requirements.

**Current transformer architecture:**

`UblInvoiceTransformer` (`src/EInvoiceBridge.Transformation/UblInvoiceTransformer.cs`) generates UBL 2.1 XML using `System.Xml.Linq`. It receives a `FormatVersion` parameter and uses:
- `formatVersion.CustomizationId` → `<cbc:CustomizationID>` (falls back to XRechnung 3.0 if null)
- `formatVersion.ProfileId` → `<cbc:ProfileID>` (falls back to Peppol BIS 3.0 if null)

Key methods:
| Method | Purpose |
|--------|---------|
| `TransformToUblXmlAsync()` | Main orchestrator — builds full XDocument |
| `BuildPartyElement()` | Seller/buyer party XML with `EndpointID` using country-specific schemeID |
| `BuildDelivery()` | Optional Delivery element (required for intra-community K/AE tax categories) |
| `GetEndpointSchemeId()` | Maps country code to Peppol participant identifier scheme |
| `BuildTaxTotal()` | Tax breakdown with exemption reasons |
| `BuildInvoiceLine()` | Line items with allowance/charge |

XML namespaces are defined in `src/EInvoiceBridge.Transformation/XmlNamespaces.cs`:
- `Invoice` = `urn:oasis:names:specification:ubl:schema:xsd:Invoice-2`
- `Cac` = `urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2`
- `Cbc` = `urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2`

#### Path A: Peppol UBL Countries

For most Peppol countries, the existing transformer works with minimal changes:

**1. Add endpoint schemeID** — edit `GetEndpointSchemeId()` in `UblInvoiceTransformer.cs`:

```csharp
private static string GetEndpointSchemeId(string countryCode) => countryCode.ToUpperInvariant() switch
{
    "BE" => "0208",   // Belgian enterprise number (KBO/BCE) — already present
    "DE" => "9930",   // German VAT (USt-IdNr) — already present
    "FR" => "0009",   // French SIRET
    "IT" => "0211",   // Italian VAT (Partita IVA)
    "NL" => "0106",   // Dutch KvK number
    "GR" => "9933",   // Greek tax registration (AFM)
    "RO" => "9947",   // Romanian fiscal code (CIF)
    "PL" => "9945",   // Polish NIP
    "HR" => "9934",   // Croatian OIB
    "ES" => "9920",   // Spanish NIF
    _ => "9930"
};
```

> Note: `"BE" => "0208"` is already present in the codebase. For Belgium, no transformer code changes are needed.

**2. Country-specific optional XML elements** — Some CIUS require additional XML elements. Add these as conditional blocks in `TransformToUblXmlAsync()` keyed on `formatVersion.CountryCode`:

```csharp
// Example: Greece (myDATA) needs InvoiceDocumentReference with MARK number
if (formatVersion.CountryCode == "GR" && invoice.CountryExtensions?.ContainsKey("GR:MarkNumber") == true)
{
    doc.Root.Add(new XElement(Cac + "InvoiceDocumentReference",
        new XElement(Cbc + "ID", invoice.CountryExtensions["GR:MarkNumber"])));
}
```

**3. XRechnung-specific elements** — The current transformer includes some elements specific to XRechnung (Germany). For other countries, verify whether each element is required or should be conditional. Document which elements are DE-only vs. universal Peppol BIS.

#### Path B: New Transformer

When a country needs a fundamentally different XML schema, create a new class implementing `ITransformationService`:

```csharp
// src/EInvoiceBridge.Transformation/FatturaPaTransformer.cs
public sealed class FatturaPaTransformer : ITransformationService
{
    public Task<string> TransformToUblXmlAsync(
        Invoice invoice, FormatVersion formatVersion,
        CancellationToken cancellationToken = default)
    {
        // Build FatturaPA XML (completely different schema)
        // Namespace: http://ivaservizi.agenziaentrate.gov.it/docs/xsd/fatture/v1.2
        // Root element: <FatturaElettronica>
    }
}
```

##### Transformer Selection Pattern

The current DI registers a single `ITransformationService`. With multiple transformers, introduce a factory (this is triggered when the first non-UBL country is added):

```csharp
// src/EInvoiceBridge.Core/Interfaces/ITransformerFactory.cs
public interface ITransformerFactory
{
    ITransformationService GetTransformer(FormatVersion formatVersion);
}

// src/EInvoiceBridge.Transformation/TransformerFactory.cs
public sealed class TransformerFactory : ITransformerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public TransformerFactory(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    public ITransformationService GetTransformer(FormatVersion formatVersion)
        => formatVersion.FormatName switch
        {
            "XRechnung" or "Peppol BIS" => _serviceProvider.GetRequiredService<UblInvoiceTransformer>(),
            "FatturaPA" => _serviceProvider.GetRequiredService<FatturaPaTransformer>(),
            _ => throw new NotSupportedException($"No transformer for format: {formatVersion.FormatName}")
        };
}
```

**DI registration** in `src/EInvoiceBridge.Transformation/DependencyInjection.cs`:
- **Path A:** No changes — existing `UblInvoiceTransformer` handles all UBL countries
- **Path B:** Register new transformer + factory:
  ```csharp
  services.AddScoped<UblInvoiceTransformer>();
  services.AddScoped<FatturaPaTransformer>();
  services.AddScoped<ITransformerFactory, TransformerFactory>();
  ```

---

### Step 4: Delivery — Storecove Routing & Alternative Delivery Services

**What:** Configure how the generated XML reaches the buyer.

**Current architecture:**

`IDeliveryService` (`src/EInvoiceBridge.Core/Interfaces/IDeliveryService.cs`):

```csharp
public interface IDeliveryService
{
    Task<string> SubmitAsync(Guid invoiceId, string ublXml, string buyerVatNumber,
        CancellationToken cancellationToken = default);
}
```

`StorecoveDeliveryService` (`src/EInvoiceBridge.Delivery/StorecoveDeliveryService.cs`) is currently a stub that throws `NotImplementedException`. When implemented, it will POST to Storecove's `/api/v2/document_submissions` endpoint using the `StorecoveSubmissionRequest` model (`src/EInvoiceBridge.Delivery/Models/StorecoveSubmissionRequest.cs`):

```json
{
    "legalEntityId": 12345,
    "document": {
        "documentType": "invoice",
        "rawDocumentData": {
            "document": "<base64-encoded UBL XML>",
            "parseStrategy": "ubl"
        }
    },
    "routing": {
        "eIdentifiers": [
            { "scheme": "DE:VAT", "id": "DE123456789" }
        ]
    }
}
```

#### Path A: Peppol Countries via Storecove

The only change is the `eIdentifiers` scheme value per country. This routing logic should be added to `StorecoveDeliveryService` keyed on the buyer's country code. See the [Storecove Routing Schemes](#storecove-eidentifier-routing-schemes) reference table.

#### Path B: Non-Storecove Delivery

For countries with government clearance platforms, create a new delivery service:

```csharp
// src/EInvoiceBridge.Delivery/KsefDeliveryService.cs
public sealed class KsefDeliveryService : IDeliveryService
{
    private readonly HttpClient _httpClient;
    private readonly KsefOptions _options;

    public async Task<string> SubmitAsync(
        Guid invoiceId, string xml, string buyerVatNumber,
        CancellationToken cancellationToken = default)
    {
        // 1. Authenticate with KSeF (session token)
        // 2. Submit structured invoice to KSeF API
        // 3. Receive KSeF reference number
        // 4. Return reference number as submission ID
    }
}
```

Add a new configuration class following the `StorecoveOptions` pattern (`src/EInvoiceBridge.Delivery/Options/StorecoveOptions.cs`):

```csharp
// src/EInvoiceBridge.Delivery/Options/KsefOptions.cs
public sealed class KsefOptions
{
    public const string SectionName = "KSeF";
    public string BaseUrl { get; set; } = "https://ksef-test.mf.gov.pl/api";
    public string ApiKey { get; set; } = string.Empty;
}
```

**Delivery service selection** — same factory pattern as transformers:

```csharp
// src/EInvoiceBridge.Core/Interfaces/IDeliveryServiceFactory.cs
public interface IDeliveryServiceFactory
{
    IDeliveryService GetDeliveryService(FormatVersion formatVersion);
}
```

**DI registration** in `src/EInvoiceBridge.Delivery/DependencyInjection.cs`:
- **Path A:** No structural changes, just add routing logic inside `StorecoveDeliveryService`
- **Path B:** Register new delivery service + factory, bind new config section

---

### Step 5: Testing

Every new country implementation must have comprehensive tests.

**Test infrastructure:**
- `tests/EInvoiceBridge.Tests.Unit/` — xUnit + FluentAssertions + NSubstitute
- `tests/EInvoiceBridge.Tests.Integration/` — Testcontainers (PostgreSQL 16, Kafka 7.6, Redis 7)
- `tests/EInvoiceBridge.Tests.Architecture/` — NetArchTest layer dependency enforcement (automatic, no changes needed)
- Test data factory: `tests/EInvoiceBridge.Tests.Unit/InvoiceTestDataBuilder.cs`

#### Required Tests

**1. Test data builder** — Add factory methods to `InvoiceTestDataBuilder.cs`:

```csharp
public static Invoice CreateValidBelgianInvoice()
{
    // Belgian seller (BE VAT) -> Belgian buyer (BE KBO)
    // Tax category "S" (standard rate, domestic) at 21%
    // Belgian structured communication reference in BuyerReference
}

public static Invoice CreateValidBelgianCrossBorderInvoice()
{
    // Belgian seller -> German buyer
    // Tax category "K" (intra-community), 0% rate
}
```

> Reference the existing `CreateValidInvoice()` method in `InvoiceTestDataBuilder.cs` — it creates a Belgian seller → German buyer cross-border invoice with tax category "K" at 0%.

**2. Validation rule unit tests** — Create `tests/EInvoiceBridge.Tests.Unit/Validation/BelgianBusinessRuleTests.cs`:

```csharp
public class BelgianBusinessRuleTests
{
    private readonly BelgianBusinessRule _sut = new();

    [Fact]
    public async Task ValidateAsync_WithValidBelgianInvoice_ReturnsNoErrors()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidBelgianInvoice();
        var errors = await _sut.ValidateAsync(invoice);
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithNonBelgianBuyer_SkipsValidation()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice(); // DE buyer
        var errors = await _sut.ValidateAsync(invoice);
        errors.Should().BeEmpty(); // Rule self-guards
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidKboNumber_ReturnsError()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidBelgianInvoice();
        invoice.Buyer.VatNumber = "INVALID";
        var errors = await _sut.ValidateAsync(invoice);
        errors.Should().ContainSingle(e => e.Field == "Buyer.VatNumber");
    }
}
```

> Reference the existing pattern in `tests/EInvoiceBridge.Tests.Unit/Validation/GermanBusinessRuleTests.cs`.

**3. Transformation unit tests** — Add to or create alongside `tests/EInvoiceBridge.Tests.Unit/Transformation/UblInvoiceTransformerTests.cs`:

```csharp
[Fact]
public async Task TransformToUblXml_BelgianInvoice_HasCorrectCustomizationId()
{
    var invoice = InvoiceTestDataBuilder.CreateValidBelgianInvoice();
    var formatVersion = new FormatVersion
    {
        CustomizationId = "urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0",
        ProfileId = "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0",
        CountryCode = "BE"
    };

    var xml = await _sut.TransformToUblXmlAsync(invoice, formatVersion);
    var doc = XDocument.Parse(xml);

    var customizationId = doc.Root!
        .Element(Cbc + "CustomizationID")!.Value;
    customizationId.Should().Be(formatVersion.CustomizationId);
}
```

**4. External format validation** — Run generated XML through the appropriate validator:

| Country | Validator |
|---------|-----------|
| All Peppol countries | OpenPeppol validation artifacts (Schematron rules) |
| Germany (XRechnung) | KoSIT validator |
| Italy | SDI validation tool (Agenzia delle Entrate) |
| France | Chorus Pro test environment |
| Poland | KSeF sandbox API |
| Romania | ANAF e-Factura test environment |

**5. Integration tests** (when Testcontainers infrastructure is ready):
- DB round-trip: insert `format_version` → `FormatRepository.GetActiveFormatAsync("BE")` returns correct record
- End-to-end pipeline: POST Belgian invoice → validation passes → UBL XML generated with Belgian CustomizationID

---

### Step 6: DI Wiring & Configuration

**Files that wire everything together:**
- `src/EInvoiceBridge.Api/Program.cs` — calls all `.Add*()` extension methods
- `src/EInvoiceBridge.Worker/Program.cs` — identical DI registrations for Kafka consumer host
- Each project's `DependencyInjection.cs` — defines the `.Add*()` extension method

**Path A (new validation rule only):**

Single line addition to `src/EInvoiceBridge.Validation/DependencyInjection.cs`:
```csharp
services.AddScoped<IValidationRule, BelgianBusinessRule>();
```
No changes to either `Program.cs` — the `AddValidation()` call already pulls in all rules.

**Path B (new transformer + delivery service):**
1. Register new transformer in `src/EInvoiceBridge.Transformation/DependencyInjection.cs`
2. Register transformer factory in `src/EInvoiceBridge.Transformation/DependencyInjection.cs`
3. Register new delivery service in `src/EInvoiceBridge.Delivery/DependencyInjection.cs`
4. Register delivery factory in `src/EInvoiceBridge.Delivery/DependencyInjection.cs`
5. Add config section in `appsettings.json` (e.g., `"KSeF": { "BaseUrl": "...", "ApiKey": "..." }`)
6. Bind config via `services.Configure<KsefOptions>(configuration.GetSection("KSeF"))`

> **Important:** Both `Api/Program.cs` and `Worker/Program.cs` must have identical service registrations. Any new `.Add*()` call must be added to both.

---

### Step 7: Kafka Consumer Updates

The Kafka consumers in `src/EInvoiceBridge.Worker/Consumers/` are currently stubs (all throw `NotImplementedException`). When implementing them, they need to be country-aware.

**`InvoiceTransformationConsumer`** (topic: `einvoice.invoice.validated`):
- Consumes `InvoiceValidated` events
- Must: load invoice from DB → look up `FormatVersion` for buyer country via `IFormatRepository.GetActiveFormatAsync()` → call the correct transformer → store generated XML → publish `InvoiceTransformed`
- Country awareness: the `FormatVersion` determines which transformer to use (via factory if Path B)

**`InvoiceDeliveryConsumer`** (topic: `einvoice.invoice.transformed`):
- Consumes `InvoiceTransformed` events
- Must: load invoice + generated XML → determine delivery route based on buyer country → call the correct delivery service → store submission ID → publish `InvoiceSent`
- Country awareness: delivery service selection based on format (Peppol via Storecove vs. clearance platform)

**`InvoiceValidationConsumer`** (topic: `einvoice.invoice.received`):
- Must: load invoice → look up `FormatVersion` → pass both to `ValidationService.ValidateAsync()` so that only the correct country's rules are executed

**`InvoiceStatusConsumer`** (topic: `einvoice.invoice.sent`):
- Terminal status updates — generally country-agnostic, but clearance-model countries may need to poll for government acknowledgment

---

## Architecture Gaps

Honest inventory of stubs and missing infrastructure that must be addressed before multi-country works.

| Gap | What Exists | What's Missing | Files |
|-----|-------------|----------------|-------|
| **FormatRepository stubs** | Interface + SQL queries exist | `GetRulesByFormatAsync()` and `GetCodeListAsync()` return empty arrays | `src/EInvoiceBridge.Persistence/Repositories/FormatRepository.cs`, `db/queries/formats/get_rules_by_format.sql`, `db/queries/formats/get_code_list.sql` |
| **Country-aware validation** | All rules run for all invoices | `ValidationService` should filter rules by format_version's enabled rules from DB | `src/EInvoiceBridge.Validation/ValidationService.cs` |
| **Transformer selection** | Single `ITransformationService` registration | Need factory for multiple format families (Path B only) | `src/EInvoiceBridge.Transformation/DependencyInjection.cs` |
| **Delivery service** | `StorecoveDeliveryService` is a stub (throws) | Needs actual Storecove API integration + country-specific routing | `src/EInvoiceBridge.Delivery/StorecoveDeliveryService.cs` |
| **Kafka consumers** | 4 consumers exist as stubs | All `HandleAsync` methods throw `NotImplementedException` | `src/EInvoiceBridge.Worker/Consumers/*.cs` |
| **Delivery routing** | `StorecoveRouting` model exists | No logic to set eIdentifier scheme per country | `src/EInvoiceBridge.Delivery/Models/StorecoveSubmissionRequest.cs` |
| **Domain model extensions** | `Invoice` model has core fields | No `CountryExtensions` dictionary for country-specific fields | `src/EInvoiceBridge.Core/Models/Invoice.cs` |

---

## Domain Model Extensions

For country-specific fields that don't exist on the base `Invoice` model, add a `Dictionary<string, string> CountryExtensions` property. Type safety is achieved via a constants class:

```csharp
// src/EInvoiceBridge.Core/Constants/CountryFields.cs
public static class CountryFields
{
    public static class Italy
    {
        public const string CodiceDestinatario = "IT:CodiceDestinatario";
        public const string CIG = "IT:CIG";
        public const string CUP = "IT:CUP";
    }

    public static class France
    {
        public const string ServiceCode = "FR:ServiceCode";
    }

    public static class Greece
    {
        public const string MarkNumber = "GR:MarkNumber";
    }
}
```

Usage in a validator or transformer:
```csharp
var code = invoice.CountryExtensions.GetValueOrDefault(CountryFields.Italy.CodiceDestinatario);
```

Add a matching `CountryExtensionsDto` (`Dictionary<string, string>`) to `CreateInvoiceRequest` in `src/EInvoiceBridge.Core/DTOs/CreateInvoiceRequest.cs`.

---

## Implementation Checklist Template

Copy this checklist for each new country:

```markdown
## Country: [XX] - [Country Name] - [Format Name]
## Implementation Path: [ ] A (extend UBL) / [ ] B (new format/delivery)

### Database
- [ ] Flyway migration: `db/migration/V{N}__add_{xx}_format.sql`
- [ ] format_definition: inserted or reusing existing
- [ ] format_version: country_code, customization_id, profile_id, status='active'
- [ ] format_rules: one row per enabled validation rule
- [ ] code_list_entries: country-specific entries (if any)
- [ ] Tested: `dotnet test` still passes after migration

### Validation
- [ ] Country business rule: `src/EInvoiceBridge.Validation/Rules/{Country}BusinessRule.cs`
  - [ ] Implements IValidationRule with unique RuleId
  - [ ] Self-guards on buyer country code
  - [ ] Covers all country-specific mandatory fields
- [ ] Registered in `src/EInvoiceBridge.Validation/DependencyInjection.cs`
- [ ] Unit tests: `tests/EInvoiceBridge.Tests.Unit/Validation/{Country}BusinessRuleTests.cs`
  - [ ] Valid invoice -> no errors
  - [ ] Non-target country -> skips (no errors)
  - [ ] Each validation check has negative test case

### Transformation
- [ ] Endpoint schemeID: added to `GetEndpointSchemeId()` (if not already present)
- [ ] CustomizationID / ProfileID: verified in format_version seed data
- [ ] Country-specific XML elements: added with country guard (if any)
- [ ] (Path B only) New transformer class created + registered
- [ ] (Path B only) Transformer factory created + registered
- [ ] Unit tests: XML output verified for CustomizationID, schemeID, country elements

### Delivery
- [ ] Storecove eIdentifier scheme: documented and implemented in routing logic
- [ ] (Path B only) New delivery service class created
- [ ] (Path B only) Configuration section added to appsettings.json
- [ ] (Path B only) Delivery factory created + registered

### Testing & Validation
- [ ] Test data builder: `CreateValid{Country}Invoice()` method added
- [ ] Test data builder: `CreateValid{Country}CrossBorderInvoice()` method added
- [ ] All unit tests pass: `dotnet test tests/EInvoiceBridge.Tests.Unit`
- [ ] Architecture tests pass: `dotnet test tests/EInvoiceBridge.Tests.Architecture`
- [ ] External validator: run against generated XML
- [ ] Sample request JSON: created for this country (optional but recommended)

### Documentation
- [ ] `docs/FORMAT_SUPPORT.md`: status updated to "Implemented"
- [ ] This checklist: archived with completion dates
```

---

## Reference Tables

### Peppol Participant Identifier Schemes

For UBL XML `<cbc:EndpointID schemeID="...">`:

| Country | Scheme ID | Identifier Type | Example |
|---------|-----------|----------------|---------|
| DE | 9930 | VAT number (USt-IdNr) | DE123456789 |
| BE | 0208 | Enterprise number (KBO/BCE) | 0123456789 |
| FR | 0009 | SIRET | 12345678901234 |
| IT | 0211 | VAT (Partita IVA) | IT12345678901 |
| NL | 0106 | KvK number | 12345678 |
| GR | 9933 | Tax registration (AFM) | 123456789 |
| RO | 9947 | Fiscal code (CIF) | RO12345678 |
| PL | 9945 | NIP | PL1234567890 |
| HR | 9934 | OIB | 12345678901 |
| ES | 9920 | NIF | ESB12345678 |

### Storecove eIdentifier Routing Schemes

For Storecove API `routing.eIdentifiers[].scheme`:

| Country | Storecove Scheme | Notes |
|---------|-----------------|-------|
| DE | `DE:VAT` | German VAT number |
| BE | `BE:EN` | Belgian enterprise number |
| FR | `FR:SIRET` | French SIRET (14 digits) |
| IT | `IT:VAT` | Italian Partita IVA |
| NL | `NL:KVK` | Dutch Chamber of Commerce |
| PL | `PL:VAT` | Polish NIP |

> Note: Storecove scheme strings should be verified against the [Storecove API documentation](https://www.storecove.com/docs/).

### Key File Path Index

| Layer | File | Purpose |
|-------|------|---------|
| **DB** | `db/migration/V{N}__*.sql` | New Flyway migration |
| **DB** | `db/queries/formats/get_active_format.sql` | Country -> format lookup query |
| **DB** | `db/queries/formats/get_rules_by_format.sql` | Format -> rules lookup query |
| **DB** | `db/queries/formats/get_code_list.sql` | Code list lookup query |
| **Core** | `src/EInvoiceBridge.Core/Interfaces/IValidationRule.cs` | Validation rule contract |
| **Core** | `src/EInvoiceBridge.Core/Interfaces/ITransformationService.cs` | Transformer contract |
| **Core** | `src/EInvoiceBridge.Core/Interfaces/IDeliveryService.cs` | Delivery contract |
| **Core** | `src/EInvoiceBridge.Core/Interfaces/IFormatRepository.cs` | Format lookup contract |
| **Core** | `src/EInvoiceBridge.Core/Models/FormatVersion.cs` | Format version model |
| **Core** | `src/EInvoiceBridge.Core/Models/FormatRule.cs` | Rule config model |
| **Core** | `src/EInvoiceBridge.Core/DTOs/CreateInvoiceRequest.cs` | API input DTO |
| **Core** | `src/EInvoiceBridge.Core/Models/Invoice.cs` | Domain model |
| **Validation** | `src/EInvoiceBridge.Validation/Rules/GermanBusinessRule.cs` | Reference: existing country rule |
| **Validation** | `src/EInvoiceBridge.Validation/ValidationService.cs` | Rule orchestrator |
| **Validation** | `src/EInvoiceBridge.Validation/DependencyInjection.cs` | Rule registration |
| **Transform** | `src/EInvoiceBridge.Transformation/UblInvoiceTransformer.cs` | UBL XML generator |
| **Transform** | `src/EInvoiceBridge.Transformation/XmlNamespaces.cs` | XML namespace constants |
| **Transform** | `src/EInvoiceBridge.Transformation/DependencyInjection.cs` | Transformer registration |
| **Delivery** | `src/EInvoiceBridge.Delivery/StorecoveDeliveryService.cs` | Storecove submission |
| **Delivery** | `src/EInvoiceBridge.Delivery/Models/StorecoveSubmissionRequest.cs` | Routing model |
| **Delivery** | `src/EInvoiceBridge.Delivery/Options/StorecoveOptions.cs` | Storecove config |
| **Delivery** | `src/EInvoiceBridge.Delivery/DependencyInjection.cs` | Delivery registration |
| **Persistence** | `src/EInvoiceBridge.Persistence/Repositories/FormatRepository.cs` | Format DB queries |
| **Tests** | `tests/EInvoiceBridge.Tests.Unit/InvoiceTestDataBuilder.cs` | Test data factory |
| **Tests** | `tests/EInvoiceBridge.Tests.Unit/Validation/GermanBusinessRuleTests.cs` | Reference: rule test pattern |
| **Tests** | `tests/EInvoiceBridge.Tests.Unit/Transformation/UblInvoiceTransformerTests.cs` | Reference: transform test pattern |
| **Docs** | `docs/FORMAT_SUPPORT.md` | Country status tracker |

---

## Appendix A: Full Worked Example — Belgium (Path A)

This is a complete reference implementation demonstrating every step for a Peppol UBL country.

### A.1 Flyway Migration

**File:** `db/migration/V4__add_be_peppol_bis.sql`

```sql
-- Belgium: Peppol BIS 3.0 format version
-- Peppol BIS format_definition already exists (id = b0000000-0000-0000-0000-000000000002)

INSERT INTO format_versions (
    id, format_definition_id, version, country_code,
    customization_id, profile_id, status, effective_from, schema_path, created_at
)
VALUES (
    'b0000000-0000-0000-0000-000000000010'::uuid,
    'b0000000-0000-0000-0000-000000000002'::uuid,
    '3.0',
    'BE',
    'urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0',
    'urn:fdc:peppol.eu:2017:poacc:billing:01:1.0',
    'active',
    '2026-01-01',
    'Schemas/UBL-2.1',
    NOW()
);

-- Validation rules (shared EN 16931 rules + Belgian-specific)
INSERT INTO format_rules (id, format_version_id, rule_type, rule_key, priority, is_enabled, created_at)
VALUES
    (gen_random_uuid(), 'b0000000-0000-0000-0000-000000000010', 'validation', 'SchemaCompleteness',    10, TRUE, NOW()),
    (gen_random_uuid(), 'b0000000-0000-0000-0000-000000000010', 'validation', 'ArithmeticCheck',       20, TRUE, NOW()),
    (gen_random_uuid(), 'b0000000-0000-0000-0000-000000000010', 'validation', 'VatLogic',              30, TRUE, NOW()),
    (gen_random_uuid(), 'b0000000-0000-0000-0000-000000000010', 'validation', 'IdentifierFormat',      40, TRUE, NOW()),
    (gen_random_uuid(), 'b0000000-0000-0000-0000-000000000010', 'validation', 'BelgianBusinessRules',  50, TRUE, NOW());
```

### A.2 BelgianBusinessRule.cs

**File:** `src/EInvoiceBridge.Validation/Rules/BelgianBusinessRule.cs`

```csharp
using System.Text.RegularExpressions;
using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Validation.Rules;

public sealed partial class BelgianBusinessRule : IValidationRule
{
    public string RuleId => "BelgianBusinessRules";
    public int Priority => 60;

    public Task<IReadOnlyList<ValidationErrorDto>> ValidateAsync(
        Invoice invoice, CancellationToken cancellationToken = default)
    {
        if (invoice.Buyer.Address.CountryCode != "BE")
            return Task.FromResult<IReadOnlyList<ValidationErrorDto>>(
                Array.Empty<ValidationErrorDto>());

        var errors = new List<ValidationErrorDto>();

        // Belgian VAT number format: BE + 10 digits
        if (!IsValidBelgianVat(invoice.Buyer.VatNumber))
        {
            errors.Add(new ValidationErrorDto
            {
                RuleId = RuleId,
                Severity = ValidationSeverity.Error,
                Field = "Buyer.VatNumber",
                Message = "Belgian buyer VAT number must match format BE + 10 digits (e.g., BE0123456789)"
            });
        }

        // KBO enterprise number: 10 digits, first digit 0 or 1, mod 97 check
        if (!IsValidKboNumber(invoice.Buyer.VatNumber))
        {
            errors.Add(new ValidationErrorDto
            {
                RuleId = RuleId,
                Severity = ValidationSeverity.Warning,
                Field = "Buyer.VatNumber",
                Message = "Belgian KBO enterprise number failed mod-97 check digit validation"
            });
        }

        // Structured communication reference (+++DDD/DDDD/DDDDD+++) — validate if present
        if (!string.IsNullOrEmpty(invoice.BuyerReference)
            && StructuredCommPattern().IsMatch(invoice.BuyerReference)
            && !IsValidStructuredCommunication(invoice.BuyerReference))
        {
            errors.Add(new ValidationErrorDto
            {
                RuleId = RuleId,
                Severity = ValidationSeverity.Error,
                Field = "BuyerReference",
                Message = "Belgian structured communication reference has invalid check digits"
            });
        }

        return Task.FromResult<IReadOnlyList<ValidationErrorDto>>(errors);
    }

    private static bool IsValidBelgianVat(string? vatNumber)
    {
        if (string.IsNullOrWhiteSpace(vatNumber)) return false;
        var digits = vatNumber.StartsWith("BE", StringComparison.OrdinalIgnoreCase)
            ? vatNumber[2..] : vatNumber;
        return digits.Length == 10 && digits.All(char.IsDigit);
    }

    private static bool IsValidKboNumber(string? vatNumber)
    {
        if (string.IsNullOrWhiteSpace(vatNumber)) return false;
        var digits = vatNumber.StartsWith("BE", StringComparison.OrdinalIgnoreCase)
            ? vatNumber[2..] : vatNumber;
        if (digits.Length != 10 || !digits.All(char.IsDigit)) return false;

        var number = long.Parse(digits[..8]);
        var checkDigit = int.Parse(digits[8..]);
        return 97 - (number % 97) == checkDigit;
    }

    private static bool IsValidStructuredCommunication(string reference)
    {
        // Extract digits from +++DDD/DDDD/DDDDD+++
        var digits = new string(reference.Where(char.IsDigit).ToArray());
        if (digits.Length != 12) return false;

        var number = long.Parse(digits[..10]);
        var checkDigit = int.Parse(digits[10..]);
        var mod = number % 97;
        return (mod == 0 ? 97 : mod) == checkDigit;
    }

    [GeneratedRegex(@"^\+{3}\d{3}/\d{4}/\d{5}\+{3}$")]
    private static partial Regex StructuredCommPattern();
}
```

### A.3 BelgianBusinessRuleTests.cs

**File:** `tests/EInvoiceBridge.Tests.Unit/Validation/BelgianBusinessRuleTests.cs`

```csharp
using EInvoiceBridge.Core.DTOs;
using EInvoiceBridge.Validation.Rules;
using FluentAssertions;

namespace EInvoiceBridge.Tests.Unit.Validation;

public class BelgianBusinessRuleTests
{
    private readonly BelgianBusinessRule _sut = new();

    [Fact]
    public async Task ValidateAsync_WithValidBelgianInvoice_ReturnsNoErrors()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidBelgianInvoice();
        var errors = await _sut.ValidateAsync(invoice);
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithNonBelgianBuyer_SkipsValidation()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidInvoice(); // DE buyer
        var errors = await _sut.ValidateAsync(invoice);
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidBelgianVat_ReturnsError()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidBelgianInvoice();
        invoice.Buyer.VatNumber = "INVALID";
        var errors = await _sut.ValidateAsync(invoice);
        errors.Should().Contain(e => e.Field == "Buyer.VatNumber"
            && e.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidKboCheckDigit_ReturnsWarning()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidBelgianInvoice();
        invoice.Buyer.VatNumber = "BE0000000000"; // Invalid mod-97
        var errors = await _sut.ValidateAsync(invoice);
        errors.Should().Contain(e => e.Field == "Buyer.VatNumber"
            && e.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidStructuredCommunication_ReturnsError()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidBelgianInvoice();
        invoice.BuyerReference = "+++000/0000/00000+++"; // Invalid check digits
        var errors = await _sut.ValidateAsync(invoice);
        errors.Should().Contain(e => e.Field == "BuyerReference");
    }

    [Fact]
    public async Task ValidateAsync_WithValidCrossBorderInvoice_ReturnsNoErrors()
    {
        var invoice = InvoiceTestDataBuilder.CreateValidBelgianCrossBorderInvoice();
        var errors = await _sut.ValidateAsync(invoice);
        // Cross-border: buyer is not BE, so rule self-guards
        errors.Should().BeEmpty();
    }
}
```

### A.4 InvoiceTestDataBuilder Additions

Add these methods to `tests/EInvoiceBridge.Tests.Unit/InvoiceTestDataBuilder.cs`:

```csharp
public static Invoice CreateValidBelgianInvoice()
{
    return new Invoice
    {
        Id = Guid.NewGuid(),
        InvoiceNumber = "BE-2026-001",
        IssueDate = new DateOnly(2026, 3, 1),
        DueDate = new DateOnly(2026, 4, 1),
        CurrencyCode = "EUR",
        BuyerReference = "+++120/0000/01250+++",  // Valid structured communication
        Seller = new Party
        {
            Name = "Bruxelles Trading SPRL",
            VatNumber = "BE0417497106",
            Address = new Address
            {
                Street = "Rue de la Loi 42",
                City = "Brussels",
                PostalCode = "1000",
                CountryCode = "BE"
            }
        },
        Buyer = new Party
        {
            Name = "Antwerp Logistics NV",
            VatNumber = "BE0123456749",  // Valid KBO with mod-97 check
            Address = new Address
            {
                Street = "Keizerslei 10",
                City = "Antwerp",
                PostalCode = "2000",
                CountryCode = "BE"
            }
        },
        PaymentMeans = new PaymentMeans { Code = "30", Iban = "BE68539007547034" },
        Lines = new List<InvoiceLine>
        {
            new()
            {
                LineNumber = 1,
                Description = "Consulting services",
                Quantity = 10m,
                UnitCode = "HUR",
                UnitPrice = 150.00m,
                TaxCategoryCode = "S",
                TaxPercent = 21m
            }
        },
        Status = "Received",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}

public static Invoice CreateValidBelgianCrossBorderInvoice()
{
    var invoice = CreateValidBelgianInvoice();
    invoice.InvoiceNumber = "BE-XB-2026-001";
    // Change buyer to German company (cross-border)
    invoice.Buyer = new Party
    {
        Name = "Muller GmbH",
        VatNumber = "DE123456789",
        Address = new Address
        {
            Street = "Hauptstrasse 1",
            City = "Berlin",
            PostalCode = "10115",
            CountryCode = "DE"
        }
    };
    invoice.BuyerReference = "BE-XB-REF-001";
    // Intra-community: tax category K, 0%
    foreach (var line in invoice.Lines)
    {
        line.TaxCategoryCode = "K";
        line.TaxPercent = 0m;
    }
    invoice.TaxExemptionReason = "Intra-community supply - Article 138 Council Directive 2006/112/EC";
    return invoice;
}
```

### A.5 GetEndpointSchemeId — No Change Needed

Belgium (`"BE" => "0208"`) is already present in `src/EInvoiceBridge.Transformation/UblInvoiceTransformer.cs`:

```csharp
private static string GetEndpointSchemeId(string countryCode) => countryCode.ToUpperInvariant() switch
{
    "BE" => "0208",   // Already present
    "DE" => "9930",
    _ => "9930"
};
```

### A.6 DependencyInjection.cs Change

**File:** `src/EInvoiceBridge.Validation/DependencyInjection.cs`

Add one line:
```diff
  services.AddScoped<IValidationRule, GermanBusinessRule>();
+ services.AddScoped<IValidationRule, BelgianBusinessRule>();
```

### A.7 FORMAT_SUPPORT.md Update

Update Belgium's status from "Planned" to "Implemented" in `docs/FORMAT_SUPPORT.md`.

---

## Appendix B: Path B Deep Dive — Italy (FatturaPA)

This appendix outlines the additional complexity of implementing a non-UBL, clearance-model country.

### B.1 FatturaPA XML Structure

FatturaPA uses a completely different XML schema from UBL:

- **Namespace:** `http://ivaservizi.agenziaentrate.gov.it/docs/xsd/fatture/v1.2`
- **Root element:** `<p:FatturaElettronica>` (not `<Invoice>`)
- **Structure:** `FatturaElettronicaHeader` (parties, transmission data) + `FatturaElettronicaBody` (invoice data, line items, payments)
- **Key difference:** FatturaPA uses `CodiceDestinatario` (7-character recipient routing code) instead of Peppol endpoint IDs

### B.2 FatturaPaTransformer Skeleton

```csharp
// src/EInvoiceBridge.Transformation/FatturaPaTransformer.cs
using System.Xml.Linq;
using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Core.Models;

namespace EInvoiceBridge.Transformation;

public sealed class FatturaPaTransformer : ITransformationService
{
    private static readonly XNamespace Ns =
        "http://ivaservizi.agenziaentrate.gov.it/docs/xsd/fatture/v1.2";

    public Task<string> TransformToUblXmlAsync(
        Invoice invoice, FormatVersion formatVersion,
        CancellationToken cancellationToken = default)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(Ns + "FatturaElettronica",
                new XAttribute("versione", "FPR12"),
                BuildHeader(invoice),
                BuildBody(invoice)
            )
        );

        return Task.FromResult(doc.ToString());
    }

    private XElement BuildHeader(Invoice invoice)
    {
        // DatiTrasmissione, CedentePrestatore (seller), CessionarioCommittente (buyer)
        throw new NotImplementedException();
    }

    private XElement BuildBody(Invoice invoice)
    {
        // DatiGenerali, DatiBeniServizi (lines), DatiPagamento
        throw new NotImplementedException();
    }
}
```

### B.3 TransformerFactory Introduction

When Italy is added, introduce the factory:

```csharp
// src/EInvoiceBridge.Core/Interfaces/ITransformerFactory.cs
public interface ITransformerFactory
{
    ITransformationService GetTransformer(FormatVersion formatVersion);
}

// src/EInvoiceBridge.Transformation/TransformerFactory.cs
public sealed class TransformerFactory : ITransformerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public TransformerFactory(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    public ITransformationService GetTransformer(FormatVersion formatVersion)
        => formatVersion.FormatName switch
        {
            "XRechnung" or "Peppol BIS"
                => _serviceProvider.GetRequiredService<UblInvoiceTransformer>(),
            "FatturaPA"
                => _serviceProvider.GetRequiredService<FatturaPaTransformer>(),
            _ => throw new NotSupportedException(
                $"No transformer for format: {formatVersion.FormatName}")
        };
}
```

Update `src/EInvoiceBridge.Transformation/DependencyInjection.cs`:

```csharp
services.AddScoped<UblInvoiceTransformer>();
services.AddScoped<FatturaPaTransformer>();
services.AddScoped<ITransformerFactory, TransformerFactory>();
// Keep default for backward compat:
services.AddScoped<ITransformationService>(sp => sp.GetRequiredService<UblInvoiceTransformer>());
```

### B.4 IDeliveryServiceFactory

Same factory pattern for delivery:

```csharp
// src/EInvoiceBridge.Core/Interfaces/IDeliveryServiceFactory.cs
public interface IDeliveryServiceFactory
{
    IDeliveryService GetDeliveryService(FormatVersion formatVersion);
}

// src/EInvoiceBridge.Delivery/DeliveryServiceFactory.cs
public sealed class DeliveryServiceFactory : IDeliveryServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DeliveryServiceFactory(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    public IDeliveryService GetDeliveryService(FormatVersion formatVersion)
        => formatVersion.FormatName switch
        {
            "XRechnung" or "Peppol BIS"
                => _serviceProvider.GetRequiredService<StorecoveDeliveryService>(),
            "FatturaPA"
                => _serviceProvider.GetRequiredService<SdiDeliveryService>(),
            _ => throw new NotSupportedException(
                $"No delivery service for format: {formatVersion.FormatName}")
        };
}
```

### B.5 CountryFields.Italy Constants

```csharp
// src/EInvoiceBridge.Core/Constants/CountryFields.cs
public static class CountryFields
{
    public static class Italy
    {
        public const string CodiceDestinatario = "IT:CodiceDestinatario";
        public const string CIG = "IT:CIG";       // Codice Identificativo Gara (public contracts)
        public const string CUP = "IT:CUP";       // Codice Unico Progetto (public contracts)
        public const string TipoDocumento = "IT:TipoDocumento"; // TD01, TD02, etc.
    }
}
```

### B.6 ItalianBusinessRule

```csharp
// src/EInvoiceBridge.Validation/Rules/ItalianBusinessRule.cs
public sealed class ItalianBusinessRule : IValidationRule
{
    public string RuleId => "ItalianBusinessRules";
    public int Priority => 60;

    public Task<IReadOnlyList<ValidationErrorDto>> ValidateAsync(
        Invoice invoice, CancellationToken cancellationToken = default)
    {
        if (invoice.Buyer.Address.CountryCode != "IT")
            return Task.FromResult<IReadOnlyList<ValidationErrorDto>>(
                Array.Empty<ValidationErrorDto>());

        var errors = new List<ValidationErrorDto>();

        // CodiceDestinatario is mandatory for Italian invoices
        var codiceDestinatario = invoice.CountryExtensions?
            .GetValueOrDefault(CountryFields.Italy.CodiceDestinatario);
        if (string.IsNullOrWhiteSpace(codiceDestinatario))
        {
            errors.Add(new ValidationErrorDto
            {
                RuleId = RuleId,
                Severity = ValidationSeverity.Error,
                Field = "CountryExtensions.IT:CodiceDestinatario",
                Message = "CodiceDestinatario is required for Italian invoices (7-character recipient code)"
            });
        }
        else if (codiceDestinatario.Length != 7)
        {
            errors.Add(new ValidationErrorDto
            {
                RuleId = RuleId,
                Severity = ValidationSeverity.Error,
                Field = "CountryExtensions.IT:CodiceDestinatario",
                Message = "CodiceDestinatario must be exactly 7 characters"
            });
        }

        // Partita IVA format: IT + 11 digits
        if (!IsValidPartitaIva(invoice.Buyer.VatNumber))
        {
            errors.Add(new ValidationErrorDto
            {
                RuleId = RuleId,
                Severity = ValidationSeverity.Error,
                Field = "Buyer.VatNumber",
                Message = "Italian buyer VAT must be a valid Partita IVA (IT + 11 digits)"
            });
        }

        return Task.FromResult<IReadOnlyList<ValidationErrorDto>>(errors);
    }

    private static bool IsValidPartitaIva(string? vatNumber)
    {
        if (string.IsNullOrWhiteSpace(vatNumber)) return false;
        var digits = vatNumber.StartsWith("IT", StringComparison.OrdinalIgnoreCase)
            ? vatNumber[2..] : vatNumber;
        return digits.Length == 11 && digits.All(char.IsDigit);
    }
}
```

### B.7 Flyway Migration for Italy

```sql
-- V{N}__add_it_fatturapa.sql

-- New format_definition for FatturaPA
INSERT INTO format_definitions (id, name, description, base_standard, syntax, created_at)
VALUES (
    'b0000000-0000-0000-0000-000000000003'::uuid,
    'FatturaPA',
    'Italian electronic invoicing format (FatturaPA 1.2)',
    'EN 16931',
    'FatturaPA XML',
    NOW()
);

-- Italian format_version
INSERT INTO format_versions (
    id, format_definition_id, version, country_code,
    customization_id, profile_id, status, effective_from, schema_path, created_at
)
VALUES (
    'b0000000-0000-0000-0000-000000000020'::uuid,
    'b0000000-0000-0000-0000-000000000003'::uuid,
    '1.2',
    'IT',
    'http://ivaservizi.agenziaentrate.gov.it/docs/xsd/fatture/v1.2',
    NULL,  -- FatturaPA doesn't use Peppol ProfileID
    'active',
    '2019-01-01',
    'Schemas/FatturaPA-1.2',
    NOW()
);

-- Validation rules for IT
INSERT INTO format_rules (id, format_version_id, rule_type, rule_key, priority, is_enabled, created_at)
VALUES
    (gen_random_uuid(), 'b0000000-0000-0000-0000-000000000020', 'validation', 'SchemaCompleteness',    10, TRUE, NOW()),
    (gen_random_uuid(), 'b0000000-0000-0000-0000-000000000020', 'validation', 'ArithmeticCheck',       20, TRUE, NOW()),
    (gen_random_uuid(), 'b0000000-0000-0000-0000-000000000020', 'validation', 'VatLogic',              30, TRUE, NOW()),
    (gen_random_uuid(), 'b0000000-0000-0000-0000-000000000020', 'validation', 'IdentifierFormat',      40, TRUE, NOW()),
    (gen_random_uuid(), 'b0000000-0000-0000-0000-000000000020', 'validation', 'ItalianBusinessRules',  50, TRUE, NOW());
```
