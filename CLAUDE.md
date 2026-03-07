# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Broen** (Danish for "bridge") is a Proof of Concept for a cross-border B2B e-invoicing platform. It targets **Germany** as the first country, implementing: API ingestion → validation → UBL 2.1 XML transformation → delivery via Storecove sandbox → status tracking.

The full design specification lives in `docs/PEPPOL-CLAUDE_CODE_POC_INSTRUCTIONS.md` — read it before implementing any feature.

## Tech Stack

- .NET 8, C#, ASP.NET Core Web API
- PostgreSQL (via Docker Compose)
- Entity Framework Core
- No frontend — API-only POC
- Single-tenant, API key auth only

## Solution Structure

```
src/
  EInvoiceBridge.Api/              # Web API (controllers, Program.cs)
  EInvoiceBridge.Core/             # Domain models, DTOs, interfaces
  EInvoiceBridge.Validation/       # Validation pipeline (rule-based)
  EInvoiceBridge.Transformation/   # UBL 2.1 XML generation via System.Xml.Linq
  EInvoiceBridge.Delivery/         # Storecove sandbox HTTP client
  EInvoiceBridge.Persistence/      # EF Core + PostgreSQL
tests/
  EInvoiceBridge.Tests.Unit/
  EInvoiceBridge.Tests.Integration/
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

# Docker Compose (API + PostgreSQL)
docker compose up --build

# EF Core migrations
dotnet ef migrations add <Name> --project src/EInvoiceBridge.Persistence --startup-project src/EInvoiceBridge.Api
dotnet ef database update --project src/EInvoiceBridge.Persistence --startup-project src/EInvoiceBridge.Api

# User secrets for Storecove API key
dotnet user-secrets set "Storecove:ApiKey" "<key>" --project src/EInvoiceBridge.Api
```

## Architecture & Pipeline

The invoice processing flow is synchronous for the POC:

1. **POST /api/invoices** receives `CreateInvoiceRequest` JSON (our canonical schema, not UBL)
2. JSON is mapped to domain models (`Invoice`, `InvoiceLine`, `Party`, etc. in Core)
3. **Validation pipeline** runs all `IValidationRule` implementations in sequence, aggregating errors/warnings. Rules: schema completeness, arithmetic, VAT logic (cross-border B2B), identifier format, XRechnung-specific
4. **UBL 2.1 XML transformation** via `System.Xml.Linq` (XDocument/XElement) — never string concatenation. Output must conform to Peppol BIS Billing 3.0 + XRechnung CIUS
5. **XSD validation** against UBL 2.1 schemas (stored locally under `Schemas/`)
6. **Storecove delivery** — base64-encoded UBL XML submitted to `POST /api/v2/document_submissions`
7. **Webhook** at `POST /api/webhooks/storecove` receives delivery status updates

Status progression: `Received → Validating → Valid/Invalid → Transforming → Sending → Sent → Delivered/Failed`

## Key Domain Rules

- **XRechnung**: `BuyerReference` (BT-10) is mandatory. CustomizationID must be `urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0`
- **Cross-border B2B** (seller country ≠ buyer country, both EU): tax category must be "K" (intra-community) or "AE" (reverse charge), tax percent = 0, exemption reason required
- **XML amounts**: always 2 decimal places, always include `currencyID` attribute
- **Peppol endpoint schemeIDs**: "0208" for Belgian VAT, "9930" for German VAT numbers
- **UBL namespaces** are critical — use the constants in `XmlNamespaces.cs`

## Configuration

Storecove settings in `appsettings.json` under `"Storecove"` key (BaseUrl, ApiKey, LegalEntityId, WebhookSecret). API keys must never be committed — use .NET User Secrets or environment variables.

Docker Compose exposes: API on port 5000, PostgreSQL on port 5432.
