# E-Invoicing Bridge POC — Germany (XRechnung via Peppol)

## Claude Code Instructions

You are helping build a Proof of Concept for a cross-border B2B e-invoicing platform. This POC targets **Germany** as the first country and implements the full pipeline: API ingestion → validation → UBL 2.1 XML transformation → delivery via Storecove sandbox → status tracking.

**Read this entire document before writing any code.** Ask clarifying questions if anything is ambiguous. Implement incrementally — get each phase working and tested before moving to the next.

---

## 1. Project Overview

### What We're Building

A .NET 8 Web API that:

1. Accepts invoice data via a REST endpoint (normalized JSON schema)
2. Validates the invoice against German/EU business rules
3. Transforms the data into a UBL 2.1 XML document compliant with Peppol BIS Billing 3.0 and the German CIUS (XRechnung)
4. Sends the XML to Storecove's sandbox API for Peppol network delivery
5. Tracks invoice status via webhooks
6. Provides a simple status query endpoint

### What We're NOT Building (Yet)

- No frontend dashboard (API-only for POC)
- No ERP connectors (manual API calls or test scripts)
- No multi-country support (Germany only)
- No user authentication (API key header only for POC)
- No multi-tenancy (single-tenant)
- No production Peppol connectivity (Storecove sandbox only)

### Tech Stack

- **.NET 8** (C#, ASP.NET Core Web API)
- **PostgreSQL** for invoice storage and audit trail
- **Entity Framework Core** as ORM
- No Redis needed for POC (in-memory caching is fine)
- No message queue for POC (synchronous processing, async delivery)
- **Docker Compose** for local dev (API + PostgreSQL)

---

## 2. Solution Structure

```
src/
├── EInvoiceBridge.Api/              # ASP.NET Core Web API project
│   ├── Controllers/
│   │   ├── InvoicesController.cs    # POST /invoices, GET /invoices/{id}, GET /invoices/{id}/status
│   │   └── WebhooksController.cs   # POST /webhooks/storecove (delivery status callbacks)
│   ├── Program.cs
│   └── appsettings.json
│
├── EInvoiceBridge.Core/             # Domain models, interfaces, DTOs
│   ├── Models/
│   │   ├── Invoice.cs               # Internal domain model
│   │   ├── InvoiceLine.cs
│   │   ├── Party.cs                 # Seller/Buyer
│   │   ├── TaxBreakdown.cs
│   │   └── InvoiceStatus.cs         # Enum: Received, Validating, Valid, Invalid, Transforming, Sending, Delivered, Failed
│   ├── DTOs/
│   │   ├── CreateInvoiceRequest.cs  # The JSON schema clients POST
│   │   ├── InvoiceResponse.cs       # What we return
│   │   └── ValidationResult.cs
│   └── Interfaces/
│       ├── IValidationService.cs
│       ├── ITransformationService.cs
│       └── IDeliveryService.cs
│
├── EInvoiceBridge.Validation/       # Validation engine
│   ├── ValidationService.cs
│   ├── Rules/
│   │   ├── SchemaCompletenessRule.cs
│   │   ├── ArithmeticRule.cs
│   │   ├── VatLogicRule.cs
│   │   ├── IdentifierFormatRule.cs
│   │   └── GermanBusinessRule.cs    # XRechnung-specific rules
│   └── IValidationRule.cs           # Interface for rule pipeline
│
├── EInvoiceBridge.Transformation/   # XML generation
│   ├── UblInvoiceTransformer.cs     # Builds UBL 2.1 XML from domain model
│   ├── XmlNamespaces.cs             # UBL/CAC/CBC namespace constants
│   └── CodeLists/                   # Enum mappings for UBL code values
│       ├── CurrencyCode.cs
│       ├── UnitCode.cs
│       ├── TaxCategoryCode.cs
│       └── InvoiceTypeCode.cs
│
├── EInvoiceBridge.Delivery/         # Storecove integration
│   ├── StorecoveDeliveryService.cs
│   ├── StorecoveClient.cs           # HTTP client wrapper
│   ├── Models/
│   │   ├── StorecoveSubmission.cs   # Storecove API request model
│   │   └── StorecoveResponse.cs     # Storecove API response model
│   └── StorecoveOptions.cs          # Config: API key, base URL, etc.
│
├── EInvoiceBridge.Persistence/      # EF Core + PostgreSQL
│   ├── AppDbContext.cs
│   ├── Migrations/
│   └── Entities/
│       ├── InvoiceEntity.cs
│       ├── InvoiceLineEntity.cs
│       └── InvoiceAuditEntry.cs     # Timestamped log per invoice
│
└── tests/
    ├── EInvoiceBridge.Tests.Unit/
    │   ├── Validation/               # Test each validation rule
    │   └── Transformation/           # Test XML output against XSD
    └── EInvoiceBridge.Tests.Integration/
        └── StorecoveIntegrationTests.cs
```

---

## 3. Data Models

### 3.1 Input: CreateInvoiceRequest (JSON the client POSTs)

Design a clean JSON schema that captures all fields needed for a German B2B invoice. This is YOUR canonical schema — not UBL, not XRechnung. It should be human-readable and ERP-agnostic.

Required fields for German B2B:

```json
{
  "invoiceNumber": "INV-2026-0847",
  "issueDate": "2026-03-06",
  "dueDate": "2026-04-05",
  "invoiceTypeCode": "380",          // 380 = Commercial Invoice, 381 = Credit Note
  "currencyCode": "EUR",
  "buyerReference": "PO-2026-1234",  // BT-10: Required by XRechnung for B2B if no Leitweg-ID
  
  "seller": {
    "name": "Van Houten Industrial BV",
    "vatNumber": "BE0123456789",
    "address": {
      "street": "Industrielaan 42",
      "city": "Ghent",
      "postalCode": "9000",
      "countryCode": "BE"
    },
    "contact": {
      "name": "Ingrid Peeters",
      "email": "ingrid@vanhouten.be"
    }
  },
  
  "buyer": {
    "name": "Müller GmbH",
    "vatNumber": "DE123456789",
    "address": {
      "street": "Hauptstraße 15",
      "city": "Stuttgart",
      "postalCode": "70173",
      "countryCode": "DE"
    }
  },
  
  "paymentMeans": {
    "code": "30",                     // 30 = Credit transfer
    "iban": "BE68539007547034",
    "bic": "BBRUBEBB"
  },
  
  "lines": [
    {
      "lineNumber": 1,
      "description": "Hydraulic fitting HF-2240",
      "quantity": 500,
      "unitCode": "C62",              // C62 = unit/piece (UN/ECE Recommendation 20)
      "unitPrice": 22.50,
      "discount": {
        "amount": 562.50,
        "reason": "Volume discount 5%"
      },
      "taxCategoryCode": "K",         // K = Intra-community supply (reverse charge)
      "taxPercent": 0
    },
    {
      "lineNumber": 2,
      "description": "Hydraulic fitting HF-3100",
      "quantity": 100,
      "unitCode": "C62",
      "unitPrice": 13.50,
      "discount": {
        "amount": 67.50,
        "reason": "Volume discount 5%"
      },
      "taxCategoryCode": "K",
      "taxPercent": 0
    }
  ],
  
  "taxExemptionReason": "Intra-community supply — Article 138 Council Directive 2006/112/EC",
  
  "notes": "Delivery ref: DEL-2026-0412"
}
```

### 3.2 Internal Domain Model

Map the above JSON to a clean C# domain model (Invoice, InvoiceLine, Party, Address, PaymentMeans, etc.). The domain model is what passes through the validation and transformation pipeline.

### 3.3 Database Entity

The database stores: the original JSON (as JSONB column for audit), the current status (enum), the generated XML (text), Storecove submission ID, delivery timestamps, and an audit trail (list of status changes with timestamps).

---

## 4. Validation Engine

### Architecture

Use a pipeline pattern: `IValidationRule` interface with a `Validate(Invoice invoice)` method returning `ValidationResult` (pass/fail + list of errors). The `ValidationService` runs all rules in sequence and aggregates results.

### Rules to Implement

**Rule 1: Schema Completeness**
- Invoice number is present and non-empty
- Issue date is present and valid
- Due date is present and >= issue date
- Seller name, VAT number, address (street, city, postal, country) are present
- Buyer name, VAT number, address are present
- At least one invoice line
- Each line has description, quantity > 0, unitPrice >= 0, taxCategoryCode
- PaymentMeans with at least IBAN
- CurrencyCode is a valid ISO 4217 code (for POC: just check it's "EUR")

**Rule 2: Arithmetic Validation**
- For each line: lineNetAmount = (quantity × unitPrice) - discount.amount
- Sum of all lineNetAmounts = invoice subtotal
- Per tax category: taxableAmount × taxPercent / 100 = taxAmount
- Subtotal + totalTax = payableAmount
- All amounts should be rounded to 2 decimal places

**Rule 3: VAT Logic (Cross-Border B2B)**
- If seller country ≠ buyer country AND both are EU countries:
  - Tax category should be "K" (intra-community) or "AE" (reverse charge)
  - Tax percent should be 0
  - taxExemptionReason must be present
- If seller country == buyer country:
  - Tax category should be "S" (standard rated) with a valid rate
  - (For POC: just validate the cross-border case since that's our focus)

**Rule 4: Identifier Format**
- Belgian VAT: `BE` + 10 digits
- German VAT (USt-IdNr): `DE` + 9 digits
- IBAN: 2 letter country code + 2 check digits + up to 30 alphanumeric (basic regex validation)
- Generic: VAT number starts with a valid EU country code prefix

**Rule 5: German/XRechnung Specific**
- BT-10 (buyerReference) is mandatory for XRechnung unless a Leitweg-ID is provided
- If buyer is a German public entity: Leitweg-ID format must be valid (for POC: skip this, we focus on B2B)
- InvoiceTypeCode must be 380 (invoice) or 381 (credit note) — validate against allowed values

### Validation Response Format

```json
{
  "isValid": false,
  "errors": [
    {
      "ruleId": "VAT_LOGIC_001",
      "severity": "error",
      "field": "taxExemptionReason",
      "message": "Intra-community supply (seller BE, buyer DE) requires a tax exemption reason (BT-120/BT-121)"
    }
  ],
  "warnings": [
    {
      "ruleId": "IDENT_WARN_001",
      "severity": "warning",
      "field": "buyer.vatNumber",
      "message": "Consider VIES validation to confirm buyer VAT number is active"
    }
  ]
}
```

---

## 5. UBL 2.1 XML Transformation

### Target Format

The output is a valid UBL 2.1 Invoice XML document, conforming to:
- **Peppol BIS Billing 3.0** (the Peppol customization of EN 16931)
- **XRechnung CIUS** (the German national customization)

### Key XML Structure

The UBL 2.1 Invoice uses these namespaces:

```xml
<Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
         xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"
         xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2">
```

### Required UBL Elements for Peppol BIS 3.0 + XRechnung

Build the XML programmatically using `System.Xml.Linq` (XDocument/XElement). Do NOT use string concatenation or templates. XLinq ensures well-formed XML.

Here is the element structure to implement (in order):

```
Invoice
├── cbc:CustomizationID = "urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0"
├── cbc:ProfileID = "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0"
├── cbc:ID = {invoiceNumber}
├── cbc:IssueDate = {issueDate, format YYYY-MM-DD}
├── cbc:DueDate = {dueDate}
├── cbc:InvoiceTypeCode = {invoiceTypeCode}
├── cbc:Note = {notes} (optional)
├── cbc:DocumentCurrencyCode = {currencyCode}
├── cbc:BuyerReference = {buyerReference}  ← BT-10, mandatory for XRechnung
│
├── cac:AccountingSupplierParty
│   └── cac:Party
│       ├── cbc:EndpointID @schemeID="0208" = {seller.vatNumber without country prefix}
│       ├── cac:PartyName / cbc:Name = {seller.name}
│       ├── cac:PostalAddress
│       │   ├── cbc:StreetName = {seller.address.street}
│       │   ├── cbc:CityName = {seller.address.city}
│       │   ├── cbc:PostalZone = {seller.address.postalCode}
│       │   └── cac:Country / cbc:IdentificationCode = {seller.address.countryCode}
│       ├── cac:PartyTaxScheme
│       │   ├── cbc:CompanyID = {seller.vatNumber}
│       │   └── cac:TaxScheme / cbc:ID = "VAT"
│       └── cac:PartyLegalEntity / cbc:RegistrationName = {seller.name}
│
├── cac:AccountingCustomerParty
│   └── cac:Party
│       ├── cbc:EndpointID @schemeID="0204" = {buyer.vatNumber without country prefix}
│       ├── cac:PartyName / cbc:Name = {buyer.name}
│       ├── cac:PostalAddress (same structure as seller)
│       ├── cac:PartyTaxScheme
│       │   ├── cbc:CompanyID = {buyer.vatNumber}
│       │   └── cac:TaxScheme / cbc:ID = "VAT"
│       └── cac:PartyLegalEntity / cbc:RegistrationName = {buyer.name}
│
├── cac:PaymentMeans
│   ├── cbc:PaymentMeansCode = {paymentMeans.code}
│   └── cac:PayeeFinancialAccount
│       ├── cbc:ID = {paymentMeans.iban}
│       └── (optionally) cac:FinancialInstitutionBranch / cbc:ID = {paymentMeans.bic}
│
├── cac:TaxTotal
│   ├── cbc:TaxAmount @currencyID = {totalTaxAmount}
│   └── cac:TaxSubtotal (one per tax category)
│       ├── cbc:TaxableAmount @currencyID = {taxableAmount}
│       ├── cbc:TaxAmount @currencyID = {taxAmount}
│       └── cac:TaxCategory
│           ├── cbc:ID = {taxCategoryCode}  ("K" for intra-community)
│           ├── cbc:Percent = {taxPercent}
│           ├── cbc:TaxExemptionReasonCode = "vatex-eu-ic" (for intra-community)
│           ├── cbc:TaxExemptionReason = {taxExemptionReason}
│           └── cac:TaxScheme / cbc:ID = "VAT"
│
├── cac:LegalMonetaryTotal
│   ├── cbc:LineExtensionAmount @currencyID = {sumOfLineNetAmounts}
│   ├── cbc:TaxExclusiveAmount @currencyID = {subtotalBeforeTax}
│   ├── cbc:TaxInclusiveAmount @currencyID = {subtotal + tax}
│   ├── cbc:AllowanceTotalAmount @currencyID = "0.00" (if no document-level discounts)
│   └── cbc:PayableAmount @currencyID = {totalPayable}
│
└── cac:InvoiceLine (one per line item)
    ├── cbc:ID = {lineNumber}
    ├── cbc:InvoicedQuantity @unitCode = {quantity}
    ├── cbc:LineExtensionAmount @currencyID = {lineNetAmount}
    ├── (if discount) cac:AllowanceCharge
    │   ├── cbc:ChargeIndicator = "false"
    │   ├── cbc:AllowanceChargeReason = {discount.reason}
    │   └── cbc:Amount @currencyID = {discount.amount}
    ├── cac:Item
    │   ├── cbc:Name = {description}
    │   └── cac:ClassifiedTaxCategory
    │       ├── cbc:ID = {taxCategoryCode}
    │       ├── cbc:Percent = {taxPercent}
    │       └── cac:TaxScheme / cbc:ID = "VAT"
    └── cac:Price
        └── cbc:PriceAmount @currencyID = {unitPrice}
```

### Important Notes for XML Generation

- All monetary amounts MUST include `@currencyID="EUR"` attribute
- All amounts must be formatted with exactly 2 decimal places (e.g., "11970.00")
- Dates in format YYYY-MM-DD
- The `schemeID` for EndpointID varies: "0208" for Belgian enterprises (KBO number), "0204" for German enterprises (Leitweg-ID) — **for POC, use "0208" for Belgian VAT, "9930" for German VAT numbers** as electronic address scheme identifiers in Peppol
- XRechnung 3.0 CustomizationID: `urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0`
- Peppol ProfileID: `urn:fdc:peppol.eu:2017:poacc:billing:01:1.0`

### XSD Validation

After generating the XML, validate it against the UBL 2.1 XSD schemas. Download the official schemas:
- UBL 2.1 schemas: http://docs.oasis-open.org/ubl/os-UBL-2.1/UBL-2.1.zip
- Extract and reference `xsd/maindoc/UBL-Invoice-2.1.xsd` and its dependencies

Use `System.Xml.Schema.XmlSchemaSet` to validate programmatically. If XSD validation fails, the invoice MUST NOT be sent — return errors to the caller.

For the POC, download and include the XSD files in the project under a `Schemas/` directory.

### Schematron Validation (Stretch Goal)

Peppol BIS 3.0 and XRechnung have Schematron rules that go beyond XSD (business rule validation). For the POC, the custom validation rules in our ValidationService cover the most critical business rules. Full Schematron validation can be added later — note this for future work.

---

## 6. Storecove Sandbox Integration

### Setup

1. Request a free 30-day sandbox account at https://www.storecove.com/us/en/start-now/
2. After account creation, generate an API key in the Storecove dashboard
3. Base URL for sandbox: `https://api.storecove.com/api/v2` (same as production but with sandbox credentials)

### API Flow

Storecove expects you to send already-formed invoice data (they accept both their own JSON format AND raw UBL XML). For the POC we'll send UBL XML since we're generating it ourselves and want to validate our transformation is correct.

**Sending an invoice:**

```
POST https://api.storecove.com/api/v2/document_submissions
Authorization: Bearer {api_key}
Content-Type: application/json

{
  "legalEntityId": {your_legal_entity_id},
  "document": {
    "documentType": "invoice",
    "rawDocumentData": {
      "document": "{base64_encoded_UBL_XML}",
      "parseStrategy": "ubl"
    }
  },
  "routing": {
    "eIdentifiers": [
      {
        "scheme": "DE:VAT",
        "id": "DE123456789"
      }
    ],
    "emails": ["fallback@example.com"]
  }
}
```

**NOTE:** The exact Storecove API schema may differ slightly — consult their docs at https://www.storecove.com/docs/ when implementing. The structure above is indicative. The key concept is:
- You provide the raw UBL XML as base64
- You specify routing info (how to find the recipient)
- Storecove handles the Peppol delivery

**Receiving status updates:**

Configure a webhook URL in Storecove dashboard. They'll POST status updates:

```json
{
  "document_submission_id": "abc123",
  "status": "delivered",           // or "failed", "accepted", etc.
  "timestamp": "2026-03-06T14:30:00Z",
  "details": { ... }
}
```

### Configuration

Store Storecove config in `appsettings.json`:

```json
{
  "Storecove": {
    "BaseUrl": "https://api.storecove.com/api/v2",
    "ApiKey": "",                    // Set via user secrets or env var, NEVER commit
    "LegalEntityId": 0,             // Your Storecove legal entity ID
    "WebhookSecret": ""             // For validating incoming webhooks
  }
}
```

Use .NET User Secrets for the API key during development:
```bash
dotnet user-secrets set "Storecove:ApiKey" "your-sandbox-api-key"
```

---

## 7. API Endpoints

### POST /api/invoices

Accepts a `CreateInvoiceRequest` JSON body. Returns the created invoice with its ID and initial status.

Flow:
1. Deserialize and map to domain model
2. Store original JSON in database (status: Received)
3. Run validation pipeline
4. If validation fails: update status to Invalid, return 422 with validation errors
5. If validation passes: update status to Valid
6. Transform to UBL 2.1 XML
7. Store the XML in database (status: Transformed)
8. Submit to Storecove (status: Sending)
9. If Storecove accepts the submission: status to Sent, return 202 Accepted with invoice ID
10. If Storecove rejects: status to Failed, return 502 with error details

For the POC, this can be synchronous (steps 1-9 in one request). In production, steps 6-9 would be async via a message queue.

Response:

```json
{
  "id": "a1b2c3d4-...",
  "invoiceNumber": "INV-2026-0847",
  "status": "Sent",
  "storecoveSubmissionId": "abc123",
  "createdAt": "2026-03-06T10:15:00Z",
  "validationResult": { "isValid": true, "errors": [], "warnings": [] },
  "xmlPreviewUrl": "/api/invoices/a1b2c3d4-.../xml"
}
```

### GET /api/invoices/{id}

Returns the full invoice details including current status, validation result, and audit trail.

### GET /api/invoices/{id}/xml

Returns the generated UBL 2.1 XML with `Content-Type: application/xml`. Useful for debugging and verifying the transformation output.

### GET /api/invoices/{id}/status

Returns just the current status and audit trail (lightweight polling endpoint).

### POST /api/webhooks/storecove

Receives Storecove delivery status webhooks. Updates the invoice status in the database. Add an audit trail entry with the delivery timestamp and status details.

---

## 8. Docker Compose Setup

```yaml
version: '3.8'
services:
  api:
    build:
      context: .
      dockerfile: src/EInvoiceBridge.Api/Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=db;Database=einvoice;Username=einvoice;Password=einvoice_dev
      - Storecove__ApiKey=${STORECOVE_API_KEY}
      - Storecove__LegalEntityId=${STORECOVE_LEGAL_ENTITY_ID}
    depends_on:
      - db

  db:
    image: postgres:16
    environment:
      POSTGRES_DB: einvoice
      POSTGRES_USER: einvoice
      POSTGRES_PASSWORD: einvoice_dev
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

---

## 9. Implementation Order

Build in this sequence. Each phase should compile, run, and be testable before moving on.

### Phase 1: Foundation (Day 1)
1. Create the solution and project structure
2. Define all domain models and DTOs
3. Set up EF Core with PostgreSQL (entities, DbContext, initial migration)
4. Create the basic API controller with POST /api/invoices that accepts JSON, stores it, and returns a response
5. Docker Compose with API + PostgreSQL
6. Test: POST a sample invoice JSON, verify it's stored in the database

### Phase 2: Validation Engine (Day 2)
1. Implement the IValidationRule interface and ValidationService pipeline
2. Implement each validation rule with unit tests
3. Wire validation into the POST /api/invoices flow
4. Test: POST an invoice with missing fields → get 422 with specific errors
5. Test: POST an invoice with wrong VAT logic → get 422
6. Test: POST a valid invoice → get 200/202

### Phase 3: XML Transformation (Day 3-4)
1. Implement UblInvoiceTransformer using System.Xml.Linq
2. Start with the invoice header (CustomizationID, ProfileID, dates, currency)
3. Add seller and buyer party structures
4. Add payment means
5. Add tax totals and subtotals
6. Add legal monetary total
7. Add invoice lines with items, quantities, prices, discounts
8. Download UBL 2.1 XSD schemas and add XSD validation step
9. Unit test: transform a known invoice → validate output XML against XSD
10. Unit test: transform the sample German cross-border invoice → verify all XRechnung-required fields are present
11. Add GET /api/invoices/{id}/xml endpoint

### Phase 4: Storecove Delivery (Day 5)
1. Implement StorecoveClient (HttpClient wrapper)
2. Implement StorecoveDeliveryService
3. Wire delivery into the POST /api/invoices flow (after successful transformation)
4. Store Storecove submission ID in database
5. Implement webhook endpoint for status updates
6. Integration test: submit a test invoice to Storecove sandbox, verify acceptance

### Phase 5: Polish & Test (Day 6)
1. Add GET /api/invoices/{id} with full audit trail
2. Add GET /api/invoices/{id}/status
3. Add proper error handling and logging throughout
4. Add a Postman/Bruno collection or .http file with sample requests
5. Write a comprehensive test script that exercises the full flow
6. Document any issues found and next steps

---

## 10. Sample Test Scenarios

### Scenario 1: Happy Path — Belgian seller → German buyer (intra-community)
Use the sample JSON from Section 3.1 above. Should validate, transform, and deliver successfully.

### Scenario 2: Validation Failure — Missing buyer VAT
Remove the buyer.vatNumber field. Expect 422 with error on buyer.vatNumber.

### Scenario 3: Validation Failure — Wrong VAT logic
Set taxCategoryCode to "S" (standard) with 21% rate on a cross-border BE→DE invoice. Expect 422: intra-community supply should use category "K" with 0%.

### Scenario 4: Validation Failure — Arithmetic error
Set quantity=500, unitPrice=22.50 but manually provide a wrong lineNetAmount. Expect 422: arithmetic mismatch.

### Scenario 5: Domestic German invoice (stretch goal)
German seller → German buyer with 19% VAT (category "S"). Different VAT treatment, tests domestic flow.

---

## 11. Key Reference Links

- **Peppol BIS Billing 3.0 specification**: https://docs.peppol.eu/poacc/billing/3.0/
- **XRechnung standard (KoSIT)**: https://xeinkauf.de/xrechnung/
- **UBL 2.1 schema files**: http://docs.oasis-open.org/ubl/os-UBL-2.1/UBL-2.1.zip
- **EN 16931 code lists**: https://docs.peppol.eu/poacc/billing/3.0/codelist/
- **Storecove API docs**: https://www.storecove.com/docs/
- **Storecove sandbox signup**: https://www.storecove.com/us/en/start-now/
- **XRechnung validation tool (KoSIT)**: https://github.com/itplr-kosit/validator
- **Peppol BIS Billing example files**: https://github.com/OpenPEPPOL/peppol-bis-invoice-3/tree/master/rules/examples

---

## 12. Important Reminders

- **Never commit API keys.** Use .NET User Secrets or environment variables.
- **All monetary amounts in XML must have exactly 2 decimal places** and a `currencyID` attribute.
- **XML namespace handling is critical.** UBL uses multiple namespaces (cbc, cac, etc.). A single namespace error makes the document invalid.
- **Use XDocument/XElement (System.Xml.Linq)**, not XmlDocument or string building.
- **Test with the Storecove sandbox before anything else.** Request the sandbox account on day 1 so it's ready by day 5.
- **The XSD files are large** (~50+ files with includes). Download the full UBL-2.1.zip and reference from a local directory.
- **Keep the audit trail from the start.** Every status change gets a timestamped entry. This is a compliance requirement and makes debugging much easier.
- **Log the raw XML that gets sent to Storecove.** When things go wrong (and they will), you need to see exactly what was transmitted.
