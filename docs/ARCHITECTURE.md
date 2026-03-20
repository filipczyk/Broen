# Broen — System Architecture

Comprehensive architecture reference for the Broen e-invoicing bridge. For the original design specification (pre-Kafka), see [`PEPPOL-CLAUDE_CODE_POC_INSTRUCTIONS.md`](PEPPOL-CLAUDE_CODE_POC_INSTRUCTIONS.md).

---

## 1. System Overview

Broen is an asynchronous e-invoicing bridge that converts canonical JSON invoices into UBL 2.1 XML (XRechnung 3.0) and delivers them via the Peppol network through Storecove.

Two runtime hosts process invoices through a Kafka-driven pipeline:

```
                         ┌──────────────────────────────────────────────────┐
                         │                  KAFKA TOPICS                    │
                         │  einvoice.invoice.received                      │
                         │  einvoice.invoice.validated                     │
                         │  einvoice.invoice.validation-failed             │
                         │  einvoice.invoice.transformed                   │
                         │  einvoice.invoice.sent                          │
                         │  einvoice.invoice.delivered                     │
                         │  einvoice.invoice.failed                        │
                         └───────┬──────────────────────────┬──────────────┘
                                 │                          │
              ┌──────────────────┴───┐           ┌──────────┴──────────────┐
              │     API HOST         │           │     WORKER HOST         │
              │                      │           │                         │
              │  InvoicesController   │           │  ValidationConsumer     │
              │  WebhooksController   │           │  TransformationConsumer │
              │  HealthCheck          │           │  DeliveryConsumer       │
              │                      │           │  StatusConsumer         │
              └──────────┬───────────┘           └──────────┬──────────────┘
                         │                                  │
                         └──────────┬───────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │   PostgreSQL  │  Redis  │     │
                    └──────────────────────────────-┘
```

**API Host** (`EInvoiceBridge.Api`): Accepts invoice submissions, publishes events, serves status queries, handles Storecove webhooks.

**Worker Host** (`EInvoiceBridge.Worker`): Runs 4 Kafka consumers as `BackgroundService` instances — validation, transformation, delivery, and terminal status.

---

## 2. Solution Structure

9 source projects + 3 test projects, organized in strict layers enforced by NetArchTest (7 architecture tests):

```
src/
  EInvoiceBridge.Api              → DI root, controllers, middleware (depends on all)
  EInvoiceBridge.Worker           → Kafka consumer host (depends on all)
  EInvoiceBridge.Application      → MediatR commands/queries (depends on Core only)
  EInvoiceBridge.Core             → Models, DTOs, enums, events, interfaces (zero deps)
  EInvoiceBridge.Validation       → 5 IValidationRule implementations (depends on Core only)
  EInvoiceBridge.Transformation   → UBL 2.1 XML generation (depends on Core only)
  EInvoiceBridge.Delivery         → Storecove HTTP client (depends on Core only)
  EInvoiceBridge.Persistence      → Dapper repositories (depends on Core only)
  EInvoiceBridge.Infrastructure   → Kafka, Redis, Serilog, OTel (depends on Core only)
tests/
  EInvoiceBridge.Tests.Unit          → Validation, transformation, application handler tests
  EInvoiceBridge.Tests.Integration   → Testcontainers (PostgreSQL, Kafka, Redis)
  EInvoiceBridge.Tests.Architecture  → 7 NetArchTest layer enforcement tests
```

**Layer rule**: Every implementation project (Application, Validation, Transformation, Delivery, Persistence, Infrastructure) depends **only** on Core. No cross-references between implementation layers. Only Api and Worker reference all projects for DI wiring.

### DI Registration

Each project exposes an `Add{Layer}()` extension method:

| Method | Registers |
|--------|-----------|
| `AddApplication()` | MediatR handlers from assembly |
| `AddValidation()` | `IValidationService` → `ValidationService`, all 5 `IValidationRule` implementations |
| `AddTransformation()` | `ITransformationService` → `UblInvoiceTransformer`, `XsdValidator` (singleton) |
| `AddDelivery(config)` | `StorecoveOptions`, `HttpClient<StorecoveClient>` (with Bearer token), `IDeliveryService` → `StorecoveDeliveryService` |
| `AddInfrastructure(config)` | `KafkaOptions`, `IEventPublisher` → `KafkaEventPublisher`, `RedisOptions`, `ICacheService` → `RedisCacheService` |
| `AddPersistence(connStr, queryPath)` | `IDbConnectionFactory`, `IQueryLoader`, `IInvoiceRepository`, `IFormatRepository`, `IAuditRepository`, Dapper snake_case mapping |

---

## 3. Async Event Pipeline

The core processing flow, end-to-end:

### Step 1: Ingestion

```
Client → POST /api/invoices (CreateInvoiceRequest JSON)
       → InvoicesController.Create()
       → MediatR → CreateInvoiceCommandHandler
           1. request.ToModel() → Invoice domain model
           2. Serialize request to RawJson
           3. IInvoiceRepository.InsertAsync() — status: Received
           4. IAuditRepository.InsertAuditEntryAsync()
           5. IEventPublisher.PublishAsync(InvoiceReceived)
       → 202 Accepted (InvoiceResponse with ID)
```

### Step 2: Validation

```
Kafka topic: einvoice.invoice.received
→ InvoiceValidationConsumer.HandleAsync(InvoiceReceived)
    1. IInvoiceRepository.GetByIdAsync(invoiceId)
    2. Update status → Validating
    3. InvoiceReconstructor.Hydrate(dbInvoice) → full Invoice model
    4. IFormatRepository.GetActiveFormatAsync(sellerCountryCode)
    5. IValidationService.ValidateAsync(invoice) → runs 5 rules in priority order
    6a. If valid:   Update status → Valid, publish InvoiceValidated
    6b. If invalid: Update status → Invalid, publish InvoiceValidationFailed
    On error:       Update status → Failed, publish InvoiceFailed
```

### Step 3: Transformation

```
Kafka topic: einvoice.invoice.validated
→ InvoiceTransformationConsumer.HandleAsync(InvoiceValidated)
    1. IInvoiceRepository.GetByIdAsync(invoiceId)
    2. Update status → Transforming
    3. InvoiceReconstructor.Hydrate(dbInvoice)
    4. IFormatRepository.GetActiveFormatAsync(sellerCountryCode)
    5. ITransformationService.TransformToUblXmlAsync(invoice, formatVersion)
    6. Update invoice with GeneratedXml
    7. Publish InvoiceTransformed
    On error: publish InvoiceFailed
```

### Step 4: Delivery

```
Kafka topic: einvoice.invoice.transformed
→ InvoiceDeliveryConsumer.HandleAsync(InvoiceTransformed)
    1. IInvoiceRepository.GetByIdAsync(invoiceId)
    2. Validate GeneratedXml exists
    3. Update status → Sending
    4. InvoiceReconstructor.Hydrate(dbInvoice)
    5. IDeliveryService.SubmitAsync(invoiceId, xml, buyerVatNumber)
       → StorecoveDeliveryService → StorecoveClient (HTTP POST)
    6. Update status → Sent, store StorecoveSubmissionId
    7. Publish InvoiceSent(invoiceId, submissionId)
    On error: publish InvoiceFailed
```

### Step 5: Terminal Status

```
Kafka topic: einvoice.invoice.sent
→ InvoiceStatusConsumer.HandleAsync(InvoiceSent)
    1. IInvoiceRepository.GetByIdAsync(invoiceId)
    2. Update status → Delivered
    3. IAuditRepository.InsertAuditEntryAsync()
    (Terminal — no further events published)
```

### Step 6: Webhook (External Status Updates)

```
Storecove → POST /api/webhooks/storecove (StorecoveWebhookPayload)
→ WebhooksController → MediatR → ProcessWebhookCommandHandler
    1. IInvoiceRepository.GetBySubmissionIdAsync(submissionId)
    2. Map Storecove status:
       - "delivered"               → InvoiceStatus.Delivered
       - "failed"/"error"/"rejected" → InvoiceStatus.Failed
       - Other                     → ignored (no-op)
    3. Update status + audit entry
```

### Status State Machine

```
Received → Validating → Valid → Transforming → Sending → Sent → Delivered
                     ↘ Invalid                              ↘ Failed
              (any step) → Failed
```

Terminal states: `Delivered`, `Invalid`, `Failed`

### Event → Topic → Consumer Mapping

| Event | EventType | Kafka Topic | Consumer |
|-------|-----------|-------------|----------|
| `InvoiceReceived` | `invoice.received` | `einvoice.invoice.received` | `InvoiceValidationConsumer` |
| `InvoiceValidated` | `invoice.validated` | `einvoice.invoice.validated` | `InvoiceTransformationConsumer` |
| `InvoiceValidationFailed` | `invoice.validation-failed` | `einvoice.invoice.validation-failed` | *(none — terminal)* |
| `InvoiceTransformed` | `invoice.transformed` | `einvoice.invoice.transformed` | `InvoiceDeliveryConsumer` |
| `InvoiceSent` | `invoice.sent` | `einvoice.invoice.sent` | `InvoiceStatusConsumer` |
| `InvoiceDelivered` | `invoice.delivered` | `einvoice.invoice.delivered` | *(none — terminal)* |
| `InvoiceFailed` | `invoice.failed` | `einvoice.invoice.failed` | *(none — terminal)* |

### Consumer Pattern

Each consumer extends `KafkaConsumerBase<TEvent>` (a `BackgroundService`). The base class:
1. Subscribes to a single topic
2. Polls for messages in a loop
3. Deserializes JSON to `TEvent`
4. Calls `HandleAsync(event, cancellationToken)`
5. Manually commits the offset after successful processing

Consumers resolve scoped services (repositories, etc.) from the DI container per message via constructor injection.

`InvoiceReconstructor.Hydrate()` is used by the Validation, Transformation, and Delivery consumers to reconstruct the full `Invoice` domain model from the database record's `RawJson` field, then copy DB-persisted metadata (Id, Status, GeneratedXml, etc.) onto the hydrated model.

---

## 4. Kafka Infrastructure

### KafkaConsumerBase\<TEvent\>

**Namespace:** `EInvoiceBridge.Infrastructure.Kafka`

Abstract base class for all consumers:

```csharp
public abstract class KafkaConsumerBase<TEvent> : BackgroundService where TEvent : class
{
    protected abstract string Topic { get; }
    protected abstract Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
```

**Consumer configuration:**
- `GroupId`: `"einvoice-workers"`
- `AutoOffsetReset`: `Earliest`
- `EnableAutoCommit`: `false` (manual commit after processing)

### KafkaEventPublisher

**Namespace:** `EInvoiceBridge.Infrastructure.Kafka`

Implements `IEventPublisher`. Resolves topic from event's `EventType` property:

```
Topic = $"{TopicPrefix}.{event.EventType}"
      = "einvoice" + "." + "invoice.received"
      = "einvoice.invoice.received"
```

**Producer configuration:**
- `Acks`: `All`
- `EnableIdempotence`: `true`
- Message key: extracted `InvoiceId` from serialized JSON (fallback: `EventId`)

### KafkaOptions

```csharp
SectionName = "Kafka"
BootstrapServers = "localhost:9092"  // default
GroupId = "einvoice-workers"
TopicPrefix = "einvoice"
```

---

## 5. Database Schema

3 Flyway migrations in `db/migration/`:

### V1 — Core Invoice Tables

| Table | Purpose | Key Columns |
|-------|---------|-------------|
| `invoices` | Primary invoice storage | `id` (UUID PK), `invoice_number`, `status` (default 'Received'), `format_version_id` (FK), `raw_json` (JSONB), `validation_result` (JSONB), `generated_xml` (TEXT), `storecove_submission_id`, `created_at`, `updated_at` |
| `invoice_lines` | Line items | `id` (UUID PK), `invoice_id` (FK, CASCADE), `line_number`, `description`, `quantity` (18,4), `unit_code`, `unit_price` (18,4), `discount_amount` (18,2), `tax_category_code`, `tax_percent` (5,2), `line_net_amount` (18,2) |
| `invoice_audit_entries` | Status change history | `id` (UUID PK), `invoice_id` (FK, CASCADE), `status`, `message` (TEXT), `details` (JSONB), `created_at` |

**Indexes (V1):**
- `idx_invoices_invoice_number` — invoice lookup by number
- `idx_invoices_status` — status-based filtering
- `idx_invoices_created_at` — DESC, for recent invoices
- `idx_invoices_storecove_id` — partial index (`WHERE storecove_submission_id IS NOT NULL`)
- `idx_invoice_lines_invoice_id` — line lookup by invoice
- `idx_invoice_audit_entries_invoice_id` — audit lookup
- `idx_invoice_audit_entries_created_at` — `(invoice_id, created_at DESC)`

### V2 — Format Versioning & Configuration

| Table | Purpose | Key Columns |
|-------|---------|-------------|
| `countries` | EU member state reference | `code` (VARCHAR(2) PK), `name`, `is_eu` (bool) |
| `format_definitions` | Format type registry | `id` (UUID PK), `name` (UNIQUE), `description` |
| `format_versions` | Country-specific format implementations | `id` (UUID PK), `format_definition_id` (FK), `version`, `country_code` (FK), `customization_id`, `profile_id`, `status` (draft/active/deprecated), `effective_from` (DATE), `effective_until` (DATE), `schema_path` |
| `format_rules` | Validation rules per format | `id` (UUID PK), `format_version_id` (FK), `rule_type`, `rule_key`, `rule_config` (JSONB), `priority`, `is_enabled` |
| `code_lists` | Reference code lists | `id` (UUID PK), `format_version_id` (FK, nullable — NULL = universal), `list_type`, `version` |
| `code_list_entries` | Individual codes | `id` (UUID PK), `code_list_id` (FK, CASCADE), `code`, `name`, `description`, `is_active` |

**Key index:** `idx_format_versions_active` — UNIQUE partial index on `(format_definition_id, country_code) WHERE status = 'active'` — enforces one active version per format per country.

### V3 — Seed Data

- **27 EU countries** (AT, BE, BG, CY, CZ, DE, DK, EE, ES, FI, FR, GR, HR, HU, IE, IT, LT, LU, LV, MT, NL, PL, PT, RO, SE, SI, SK)
- **Format definitions**: XRechnung, Peppol BIS
- **XRechnung 3.0 for Germany** (status: active, effective from 2024-02-01)
  - CustomizationID: `urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0`
  - ProfileID: `urn:fdc:peppol.eu:2017:poacc:billing:01:1.0`
- **5 validation rules** seeded for XRechnung 3.0 (SchemaCompleteness:10, ArithmeticCheck:20, VatLogic:30, IdentifierFormat:40, GermanBusinessRules:50)
- **3 code lists**: Tax categories (UNCL5305, 9 codes), Currencies (ISO4217, 12 codes), Units (UNECERec20, 12 codes)

---

## 6. SQL Queries

9 query files in `db/queries/`, loaded by `EmbeddedQueryLoader` at runtime. The loader caches queries in a `ConcurrentDictionary` and resolves paths via `GetQueryBasePath()` — which returns `../../../../../db/queries` in development or `/app/db/queries` in Docker.

### Invoice Queries (`db/queries/invoices/`)

| File | Used By | Purpose |
|------|---------|---------|
| `get_by_id.sql` | `InvoiceRepository.GetByIdAsync()` | Fetch single invoice by UUID |
| `get_by_status.sql` | `InvoiceRepository.GetByStatusAsync()` | Fetch invoices by status with pagination (LIMIT/OFFSET, ORDER BY created_at DESC) |
| `insert_invoice.sql` | `InvoiceRepository.InsertAsync()` | Insert new invoice record with RawJson |
| `update_status.sql` | `InvoiceRepository.UpdateStatusAsync()` | Update status + COALESCE merge for validation_result, generated_xml, storecove_submission_id |
| `insert_audit_entry.sql` | `AuditRepository.InsertAuditEntryAsync()` | Log status change to audit trail |
| `get_by_submission_id.sql` | `InvoiceRepository.GetBySubmissionIdAsync()` | Lookup invoice by Storecove submission ID (for webhook processing) |

### Format Queries (`db/queries/formats/`)

| File | Used By | Purpose |
|------|---------|---------|
| `get_active_format.sql` | `FormatRepository.GetActiveFormatAsync()` | Fetch active format version for country (joins format_versions → format_definitions) |
| `get_rules_by_format.sql` | `FormatRepository.GetRulesByFormatAsync()` | Fetch enabled validation rules by format version and type, ordered by priority |
| `get_code_list.sql` | `FormatRepository.GetCodeListAsync()` | Fetch code list entries (universal or format-specific) |

---

## 7. Service Interfaces & Implementations

| Interface | Implementation | Project | Responsibility |
|-----------|---------------|---------|----------------|
| `IValidationService` | `ValidationService` | Validation | Orchestrates all validation rules in priority order |
| `IValidationRule` (x5) | See [Section 8](#8-validation-rules) | Validation | Individual validation checks |
| `ITransformationService` | `UblInvoiceTransformer` | Transformation | Broen JSON → UBL 2.1 XML (XRechnung 3.0) |
| `IDeliveryService` | `StorecoveDeliveryService` | Delivery | Orchestrates Storecove submission |
| *(concrete)* | `StorecoveClient` | Delivery | HTTP client for Storecove API |
| `IInvoiceRepository` | `InvoiceRepository` | Persistence | Invoice CRUD via Dapper |
| `IAuditRepository` | `AuditRepository` | Persistence | Audit trail entries |
| `IFormatRepository` | `FormatRepository` | Persistence | Format versions, rules, code lists |
| `IDbConnectionFactory` | `NpgsqlConnectionFactory` | Persistence | Creates `NpgsqlConnection` instances |
| `IQueryLoader` | `EmbeddedQueryLoader` | Persistence | Loads and caches SQL from `db/queries/` |
| `IEventPublisher` | `KafkaEventPublisher` | Infrastructure | Publishes integration events to Kafka |
| `ICacheService` | `RedisCacheService` | Infrastructure | Redis-backed cache (Get/Set/Remove) |

---

## 8. Validation Rules

Executed sequentially in priority order by `ValidationService`. Each rule returns a list of `ValidationErrorDto` (with `RuleId`, `Severity`, `Field`, `Message`).

| Priority | RuleId | Class | Checks |
|----------|--------|-------|--------|
| 10 | `SCHEMA` | `SchemaCompletenessRule` | Required fields: InvoiceNumber, IssueDate, DueDate, BuyerReference, Seller (Name, VatNumber, Address.CountryCode), Buyer (Name, VatNumber, Address.CountryCode), at least one Line with Description, Quantity > 0, TaxCategoryCode |
| 20 | `ARITHMETIC` | `ArithmeticRule` | Quantity > 0, UnitPrice >= 0, Discount.Amount >= 0 (if present) |
| 30 | `VAT_LOGIC` | `VatLogicRule` | Cross-border (seller ≠ buyer country): TaxCategoryCode must be "K" or "AE", TaxPercent must be 0, TaxExemptionReason required |
| 40 | `IDENTIFIER` | `IdentifierFormatRule` | VAT numbers match `^[A-Z]{2}\d+$` (2-letter country code + digits) |
| 50 | `GERMAN_BIZ` | `GermanBusinessRule` | If Buyer.Address.CountryCode == "DE": BuyerReference (BT-10) is mandatory (XRechnung requirement) |

`ValidationResultDto` aggregates results: `IsValid` = true when no Error-severity issues exist.

---

## 9. UBL Transformation

`UblInvoiceTransformer` generates UBL 2.1 XML using `System.Xml.Linq` (`XDocument`/`XElement`).

### XML Namespaces (`XmlNamespaces.cs`)

```
Invoice: urn:oasis:names:specification:ubl:schema:xsd:Invoice-2
Cac:     urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2
Cbc:     urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2
```

### Key Formatting Rules

- **All amounts**: 2 decimal places via `Fmt(decimal)` method, always with `currencyID` attribute
- **Line extension**: `NetAmount = (Quantity × UnitPrice) - Discount.Amount`
- **Tax calculation**: `TaxAmount = TaxableAmount × TaxPercent / 100`
- **Tax subtotals**: Grouped by `(TaxCategoryCode, TaxPercent)`, includes `TaxExemptionReason` for "K"/"AE"

### Peppol Endpoint SchemeIDs

| Country | Scheme | SchemeID |
|---------|--------|----------|
| DE | 9930 | German VAT |
| BE | 0208 | Belgian enterprise number |
| Others | 9930 | Default |

### Default Identifiers

- **CustomizationID**: `urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0` (overridable from `FormatVersion.CustomizationId`)
- **ProfileID**: `urn:fdc:peppol.eu:2017:poacc:billing:01:1.0` (overridable from `FormatVersion.ProfileId`)

### Delivery Element

Conditionally included when:
- Any line has TaxCategoryCode "K" (intra-community) or "AE" (reverse charge), OR
- Invoice has explicit `DeliveryDate` / `DeliveryCountryCode`

---

## 10. Storecove Integration

### Submission Flow

`StorecoveDeliveryService.SubmitAsync()`:
1. Base64-encode the UBL XML
2. Extract country code from buyer VAT number (first 2 chars)
3. Map country → Peppol scheme
4. Build `StorecoveSubmissionRequest`
5. Call `StorecoveClient.SubmitDocumentAsync()` (HTTP POST to `/document_submissions`)
6. Return submission ID

### Country → Scheme Mapping

| Country | Scheme | SchemeID |
|---------|--------|----------|
| DE | `DE:VAT` | `9930` |
| BE | `BE:EN` | `0208` |
| FR | `FR:SIRET` | — |
| IT | `IT:VAT` | — |
| Other | `EU:VAT` | `9930` |

### Request Structure

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
    ],
    "emails": []
  }
}
```

### Webhook Processing

`StorecoveWebhookPayload` fields: `DocumentSubmissionId`, `Status`, `Timestamp`, `Details`.

`ProcessWebhookCommandHandler` maps:
- `"delivered"` → `Delivered`
- `"failed"` / `"error"` / `"rejected"` → `Failed`
- Other → no-op

### Configuration (`StorecoveOptions`)

```
Section: "Storecove"
BaseUrl: "https://api.storecove.com/api/v2"
ApiKey: (from user secrets / env var — never committed)
LegalEntityId: (numeric)
WebhookSecret: (for payload verification)
```

---

## 11. API Endpoints

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| `POST` | `/api/invoices` | Submit invoice (async) → 202 Accepted | API key |
| `POST` | `/api/invoices/preview` | Validate + transform preview (sync) → 200 OK | API key |
| `GET` | `/api/invoices/{id}` | Invoice details | API key |
| `GET` | `/api/invoices/{id}/xml` | Generated UBL XML (`application/xml`) | API key |
| `GET` | `/api/invoices/{id}/status` | Status + audit trail | API key |
| `POST` | `/api/webhooks/storecove` | Storecove delivery status callback | API key |
| `GET` | `/health` | Health check | None |

**Authentication**: `X-Api-Key` header validated by `ApiKeyAuthenticationMiddleware` (skips `/health`).

**Middleware pipeline**: `GlobalExceptionHandler` → `RequestLoggingMiddleware` → `ApiKeyAuthenticationMiddleware` → Controllers

---

## 12. Docker Compose

7 services with health checks and dependency ordering:

| Service | Image | Ports | Depends On |
|---------|-------|-------|------------|
| `api` | Build from `src/EInvoiceBridge.Api/Dockerfile` | 5000:8080 | db, flyway, kafka, redis |
| `worker` | Build from `src/EInvoiceBridge.Worker/Dockerfile` | — | db, flyway, kafka, redis |
| `db` | `postgres:16-alpine` | 5432:5432 | — |
| `flyway` | `flyway/flyway:10` | — | db (one-shot migration) |
| `kafka` | `apache/kafka:3.7.0` | 9092:9092 | db |
| `redis` | `redis:7-alpine` | 6379:6379 | — |
| `kafka-ui` | `provectuslabs/kafka-ui:latest` | 8080:8080 | kafka |

**Kafka**: KRaft mode (no ZooKeeper), single node. PLAINTEXT on 29092 (internal), EXTERNAL on 9092 (host).

**Flyway**: One-shot container that runs migrations from `db/migration/` and exits. Api and Worker wait for it via `depends_on: flyway: condition: service_completed_successfully`.

**Volumes**: `pgdata` for PostgreSQL persistence.

---

## 13. Key Helper Classes

### InvoiceReconstructor

**Location:** `EInvoiceBridge.Application.Helpers.InvoiceReconstructor`

Static class that reconstructs a full `Invoice` domain model from a database record:

```csharp
public static Invoice Hydrate(Invoice dbInvoice)
```

1. Deserializes `dbInvoice.RawJson` → `CreateInvoiceRequest`
2. Converts to `Invoice` via `ToModel()` mapping
3. Copies DB-persisted fields onto the hydrated model (Id, Status, RawJson, GeneratedXml, ValidationResult, StorecoveSubmissionId, FormatVersionId, CreatedAt, UpdatedAt)

Used by: `InvoiceValidationConsumer`, `InvoiceTransformationConsumer`, `InvoiceDeliveryConsumer`

**Why it exists**: The database stores only the serialized JSON (`RawJson`) plus metadata fields. The full domain model (with nested Party, Lines, etc.) must be reconstructed from `RawJson` for validation, transformation, and delivery.

### InvoiceMappingProfile

**Location:** `EInvoiceBridge.Application.Helpers` (extension method `ToModel()`)

Converts `CreateInvoiceRequest` DTO → `Invoice` domain model. Used by `CreateInvoiceCommandHandler` and `InvoiceReconstructor.Hydrate()`.

### GetQueryBasePath()

**Location:** `Program.cs` in both Api and Worker hosts

Resolves the SQL query directory path:
- **Docker**: `/app/db/queries`
- **Development**: `../../../../../db/queries` (relative to `bin/Debug/net8.0`)

Used by `AddPersistence()` to configure `EmbeddedQueryLoader`.

---

## 14. Integration Events

All events implement `IIntegrationEvent` with auto-generated `EventId` (Guid) and `OccurredAt` (DateTime.UtcNow):

| Event | Properties | EventType |
|-------|------------|-----------|
| `InvoiceReceived` | `InvoiceId`, `InvoiceNumber` | `invoice.received` |
| `InvoiceValidated` | `InvoiceId` | `invoice.validated` |
| `InvoiceValidationFailed` | `InvoiceId`, `Reason` | `invoice.validation-failed` |
| `InvoiceTransformed` | `InvoiceId` | `invoice.transformed` |
| `InvoiceSent` | `InvoiceId`, `SubmissionId` | `invoice.sent` |
| `InvoiceDelivered` | `InvoiceId` | `invoice.delivered` |
| `InvoiceFailed` | `InvoiceId`, `Reason` | `invoice.failed` |

Topic is derived by `KafkaEventPublisher`: `{TopicPrefix}.{EventType}` → e.g., `einvoice.invoice.received`.
