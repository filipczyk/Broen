# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Broen** (Danish for "bridge") is a Proof of Concept for a cross-border B2B e-invoicing platform. It targets **Germany** as the first country, implementing: API ingestion → validation → UBL 2.1 XML transformation → delivery via Storecove sandbox → status tracking.

The full design specification lives in `docs/PEPPOL-CLAUDE_CODE_POC_INSTRUCTIONS.md` — read it before implementing any feature.

## Tech Stack

- .NET 8, C#, ASP.NET Core Web API
- PostgreSQL (via Docker Compose)
- Dapper + raw SQL (queries in `db/queries/`, migrations via Flyway)
- Kafka (KRaft mode) for async event pipeline
- Redis for caching (format versions, rules, code lists)
- MediatR for CQRS in Application layer
- No frontend — API-only POC
- Single-tenant, API key auth only

## Solution Structure

```
src/
  EInvoiceBridge.Api/              # Web API (controllers, Program.cs) — DI root
  EInvoiceBridge.Worker/           # Kafka consumer host (background processing)
  EInvoiceBridge.Application/      # MediatR commands/queries (CQRS)
  EInvoiceBridge.Core/             # Domain models, DTOs, interfaces (zero deps)
  EInvoiceBridge.Validation/       # Validation pipeline (5 IValidationRule implementations)
  EInvoiceBridge.Transformation/   # UBL 2.1 XML generation via System.Xml.Linq
  EInvoiceBridge.Delivery/         # Storecove sandbox HTTP client
  EInvoiceBridge.Persistence/      # Dapper + PostgreSQL (NpgsqlConnectionFactory)
  EInvoiceBridge.Infrastructure/   # Kafka, Redis, Serilog, OpenTelemetry
tests/
  EInvoiceBridge.Tests.Unit/
  EInvoiceBridge.Tests.Integration/
  EInvoiceBridge.Tests.Architecture/
```

## Build & Run Commands

```bash
# Restore and build
dotnet build

# Run the API
dotnet run --project src/EInvoiceBridge.Api

# Run all tests
dotnet test

# Run a single test project
dotnet test tests/EInvoiceBridge.Tests.Unit

# Run a specific test by filter
dotnet test --filter "FullyQualifiedName~ArithmeticRule"

# Docker Compose (API + Worker + PostgreSQL + Kafka + Redis + Flyway + Kafka UI)
docker compose up --build

# Flyway migrations (run automatically via Docker Compose)
# Migration files: db/migration/V1__*.sql, V2__*.sql, V3__*.sql

# User secrets for Storecove API key
dotnet user-secrets set "Storecove:ApiKey" "<key>" --project src/EInvoiceBridge.Api
```

## Architecture & Pipeline

The invoice processing flow is **asynchronous via Kafka**:

1. **POST /api/invoices** receives `CreateInvoiceRequest` JSON → publishes `InvoiceReceived` event
2. **ValidationConsumer** picks up event → runs all `IValidationRule` implementations → publishes `InvoiceValidated` or `ValidationFailed`
3. **TransformationConsumer** → UBL 2.1 XML via `System.Xml.Linq` (XDocument/XElement) → publishes `InvoiceTransformed`
4. **DeliveryConsumer** → base64-encoded UBL XML submitted to Storecove `POST /api/v2/document_submissions` → publishes `InvoiceSent` or `InvoiceFailed`
5. **StatusConsumer** → terminal status updates
6. **Webhook** at `POST /api/webhooks/storecove` receives delivery status updates

Kafka topics: `einvoice.invoice.*` (7 topics)

Status progression: `Received → Validating → Valid/Invalid → Transforming → Sending → Sent → Delivered/Failed`

Validation rules (executed in priority order): SchemaCompleteness → Arithmetic → VatLogic → IdentifierFormat → GermanBusiness

UBL XML output must conform to Peppol BIS Billing 3.0 + XRechnung CIUS. XSD validation against locally stored schemas.

## Key Domain Rules

- **XRechnung**: `BuyerReference` (BT-10) is mandatory. CustomizationID must be `urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0`
- **Cross-border B2B** (seller country ≠ buyer country, both EU): tax category must be "K" (intra-community) or "AE" (reverse charge), tax percent = 0, exemption reason required
- **XML amounts**: always 2 decimal places, always include `currencyID` attribute
- **Peppol endpoint schemeIDs**: "0208" for Belgian VAT, "9930" for German VAT numbers
- **UBL namespaces** are critical — use the constants in `XmlNamespaces.cs`

## Configuration

Storecove settings in `appsettings.json` under `"Storecove"` key (BaseUrl, ApiKey, LegalEntityId, WebhookSecret). API keys must never be committed — use .NET User Secrets or environment variables.

Docker Compose services: API (5000:8080), Worker, PostgreSQL (5432), Flyway (one-shot), Kafka (9092), Redis (6379), Kafka UI (8080).
