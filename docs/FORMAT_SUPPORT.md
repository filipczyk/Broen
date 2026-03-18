# Format Support & Development Tracking

> Single reference for Broen's source format, target formats per EU country, implementation status, and development roadmap.

---

## 1. Source Format — Broen Canonical JSON

All invoices enter Broen as a simplified camelCase JSON payload defined by the `CreateInvoiceRequest` DTO.

- **DTO**: `src/EInvoiceBridge.Core/DTOs/CreateInvoiceRequest.cs`
- **Reference example**: `request.json` (repo root)
- **Format selection**: implicit via `buyer.address.countryCode` → `IFormatRepository.GetActiveFormatAsync(countryCode)`

This is not UBL, CII, or raw EN 16931. It is a Broen-specific schema designed for easy API consumption. The transformation layer converts it to the appropriate country-specific output format.

---

## 2. Target Format Family

All targets output structured XML delivered through **Storecove**.

- **Most EU countries**: UBL 2.1 XML via Peppol BIS Billing 3.0 with a country-specific CIUS (Compliant Invoicing Specification)
- **Italy**: FatturaPA XML — Storecove handles conversion from its API input and submits to SDI
- **Spain**: Facturae XML — Storecove handles conversion; VeriFactu compliance layer planned
- **France**: Factur-X (hybrid PDF/A-3 + XML), UBL, or CII — routed through Chorus Pro (PDP)

---

## 3. Priority Country Format Matrix

Current as of March 2026. Mandate dates are web-researched from authoritative sources (see §7).

| Country | Standard | B2B Mandate Timeline | Delivery Model | Broen Status |
|---------|----------|---------------------|----------------|:------------:|
| **DE** | XRechnung 3.0 (UBL 2.1) | Receive: 2025-01-01; Issue: large 2027-01-01, all 2028-01-01 | Peppol via Storecove | **Implemented** |
| **BE** | Peppol BIS 3.0 (UBL 2.1) | 2026-01-01 (all B2B) | Peppol via Storecove | Not Started |
| **FR** | Factur-X / UBL / CII (EN 16931) | Large+mid: 2026-09-01; SME: 2027-09-01 | Chorus Pro (PDP) | Not Started |
| **PL** | KSeF (structured XML) | Large: 2026-02-01; Others: 2026-04-01; Micro: 2027-01-01 | Clearance (KSeF platform) | Not Started |
| **IT** | FatturaPA XML | Already mandatory (since 2019) | Clearance (SDI) | Not Started |
| **RO** | e-Factura (RO_CIUS, UBL) | Already mandatory (2024) | Clearance (ANAF e-Factura) | Not Started |
| **ES** | Facturae XML / UBL / CII | VeriFactu: 2027-01-01; B2B e-invoice: large 2027, all 2028 | Hybrid (VeriFactu + exchange) | Not Started |
| **GR** | myDATA (Peppol BIS + country rules) | Large: 2026-02-02; Phase B: 2026-10 | Peppol via Storecove | Not Started |
| **HR** | UBL 2.1 (Peppol BIS) | 2026-01-01 | Peppol via Storecove | Not Started |

---

## 4. Currently Implemented: XRechnung 3.0 (DE)

Full end-to-end pipeline from JSON ingestion to Storecove delivery.

### Pipeline

```
POST /api/invoices (JSON) → Kafka → Validation → Transformation → Storecove Submission
```

### Specification Compliance

- **CustomizationID**: `urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0`
- **ProfileID**: `urn:fdc:peppol.eu:2017:poacc:billing:01:1.0`
- **KoSIT validation**: 0 errors, 0 warnings

### Validation Rules (5, executed in priority order)

1. **SchemaCompleteness** — required fields present and non-empty
2. **Arithmetic** — line totals, tax calculations, invoice total consistency
3. **VatLogic** — tax category codes, cross-border B2B rules (K/AE for intra-community)
4. **IdentifierFormat** — VAT number format, IBAN, endpoint schemeIDs
5. **GermanBusiness** — BuyerReference (BT-10) mandatory, XRechnung-specific rules

### Transformation

- `UblInvoiceTransformer` generates UBL 2.1 XML via `System.Xml.Linq` (XDocument/XElement)
- XML amounts: 2 decimal places, `currencyID` attribute on all amounts
- Peppol endpoint schemeIDs: "0208" (Belgian VAT), "9930" (German VAT)

### Delivery

- `IDeliveryService` interface defined
- `StorecoveDeliveryService` implementation exists (stub — not yet calling sandbox API)
- Base64-encoded UBL XML submitted to `POST /api/v2/document_submissions`

---

## 5. Architecture Readiness for Multi-Format

The architecture was designed for multi-format expansion from the start. Current readiness:

| Component | Status | Notes |
|-----------|--------|-------|
| DB schema (`format_definitions`, `format_versions`, `format_rules`) | **Done** | Flyway migrations V1–V3 |
| 27 EU countries seeded in DB | **Done** | V3 seed data |
| `FormatVersion` model with country/customization fields | **Done** | `Core/Models/` |
| `ITransformationService` accepting `FormatVersion` | **Done** | Interface designed for multi-format |
| `IFormatRepository` (country → format lookup) | **Interface done** | Implementation is a stub |
| DB-driven rule configuration | **Schema done** | Not yet consumed by validation pipeline |
| Country-specific Storecove routing (eIdentifier schemes) | **Not implemented** | Need scheme lookup per country |
| Non-UBL format support (FatturaPA, Facturae) | **Not implemented** | Requires new transformer implementations or Storecove conversion reliance |

### Key Gaps for Next Country

To add a second country (e.g., BE), the following work is needed:

1. Implement `FormatRepository` to return active format for a given country code
2. Wire validation pipeline to load country-specific rules from DB
3. Create country-specific CIUS adjustments in transformer (or new transformer class)
4. Map country-specific Peppol endpoint schemeIDs
5. Add validation rules for country-specific business requirements

---

## 6. Suggested Implementation Priority

Ordered by mandate urgency combined with implementation effort (Peppol/UBL countries first as they reuse existing infrastructure).

### Tier 1 — Peppol UBL, low delta from DE

1. **BE** — Peppol BIS 3.0, mandatory since 2026-01-01, pure UBL 2.1, closest to existing DE implementation. Mainly needs different CustomizationID and Belgian-specific validation rules.

2. **HR** — Peppol BIS UBL 2.1, mandatory since 2026-01-01. Minimal CIUS differences.

3. **GR** — Peppol BIS with myDATA country rules, large companies since 2026-02-02. Needs myDATA-specific fields.

### Tier 2 — Different delivery or format model

4. **FR** — Factur-X/UBL via Chorus Pro (PDP model), large+mid companies 2026-09-01. Requires PDP integration or Storecove PDP routing.

5. **PL** — KSeF clearance model, large companies since 2026-02-01. Different delivery mechanism (government clearance platform), not Peppol.

6. **IT** — FatturaPA via SDI, already mandatory. Storecove handles SDI submission and FatturaPA conversion from UBL input.

7. **RO** — e-Factura (RO_CIUS UBL) via ANAF, already mandatory. Clearance model with government platform.

### Tier 3 — Later mandates

8. **ES** — Facturae/VeriFactu, mandatory 2027. Hybrid model with certified invoicing software requirements.

---

## 7. Key Sources

| Topic | Source |
|-------|--------|
| EU-wide mandate overview | [Novutech: E-invoicing in Europe 2025–2027](https://www.novutech.com/news/e-invoicing-in-europe-overview-of-mandates-2025-2027) |
| EU ViDA adoption | [EC: VAT in the Digital Age](https://taxation-customs.ec.europa.eu/news/adoption-vat-digital-age-package-2025-03-11_en) |
| Germany timeline | [VATupdate: Germany B2B mandate](https://www.vatupdate.com/2025/11/12/germany-e-invoicing-b2b-mandate-timeline-and-compliance/) |
| France mandate | [EY: France e-invoicing September 2026](https://www.ey.com/en_gl/technical/tax-alerts/french-government-announces-simplification-measures-as-part-of-september-2026-e-invoicing-mandate) |
| Poland KSeF | [VATupdate: Poland KSeF guide](https://www.vatupdate.com/2025/11/26/poland-ksef-e-invoicing-mandate-a-comprehensive-guide/) |
| Spain VeriFactu | [VATcalc: Spain VeriFactu delay to 2027](https://www.vatcalc.com/spain/spain-verifactu-delay-till-jan-2027-for-certified-e-invoicing/) |
| Storecove Peppol coverage | [Storecove: Peppol countries](https://www.storecove.com/blog/en/peppol-countries/) |
| Italy SDI via Storecove | [Storecove: SDI solution](https://www.storecove.com/us/en/solutions/sdi/) |
| EU ViDA DRR 2030 | [VATcalc: EU 2028 DRR](https://www.vatcalc.com/eu/eu-2028-digital-reporting-requirements-drr-e-invoice/) |
