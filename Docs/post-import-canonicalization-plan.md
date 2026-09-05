# Guided Post-Import Canonicalization Plan

## Goal

Implement canonicalization as a separate phase that starts only after the JSON
import has committed its `StoreProduct` changes. Complete, test, and preferably
commit each numbered step before moving to the next one.

This guide supplements [product-canonicalization-plan.md](product-canonicalization-plan.md),
which contains the broader domain rules and matching policy.

## Progress

- Steps 1–3 completed on 2026-09-05.
- Baseline before these changes: 86 tests passed.
- Verification after these changes: 89 tests passed.
- Known baseline warning: the test build reports an EF Core Relational version
  conflict between 10.0.4 and 10.0.11. Steps 1–3 did not introduce it.

## Implementation steps

### 1. Establish the baseline

**Status:** Completed.

- Run the existing test suite and record that all tests pass.
- Read `ShwapnoDairyImporter`, `ShwapnoProductMapper`,
  `ProductNormalizationService`, and `CanonicalKeyBuilder`.
- Confirm that the current mapper creates a new `Product` for every new
  `StoreProduct`.
- Do not change application behavior in this step.

**Checkpoint:** The existing behavior and passing test count are recorded in the
commit or pull-request notes.

### 2. Make the canonical-product relationship optional

**Status:** Completed.

- Change `StoreProduct.ProductId` from `Guid` to `Guid?`.
- Make the `Product` navigation nullable.
- Configure the EF Core relationship as optional.
- Generate an additive migration that makes `StoreProducts.ProductId` nullable.
- Review the migration and verify that it preserves existing rows and the
  foreign key.
- Add a persistence test showing that a `StoreProduct` can exist without a
  `Product`.

**Checkpoint:** The migration applies successfully and the new persistence test
passes.

### 3. Separate retailer mapping from canonical-product creation

**Status:** Completed.

- Remove `Product` creation from `ShwapnoProductMapper.Create`.
- Make the mapper responsible only for retailer-owned data: source SKU, source
  title, prices, stock, URL, image, and timestamps.
- Remove the temporary quantity-parser experiment from `Update`.
- Leave `ProductId` null for a newly imported listing.
- Add an import test proving that JSON import creates `StoreProduct` and initial
  `PriceHistory` records without creating `Product` records.

**Checkpoint:** Importing a new listing persists it in an unresolved state.

### 4. Complete the canonical `Product` schema

- Add `CanonicalKey`, `Variant`, and `PackageType` to `Product`.
- Store multiple variant values in a stable representation: lowercase the
  values, sort them using ordinal comparison, and join them with `+`.
- Make `PackageType` nullable.
- Add a unique database index on `CanonicalKey`.
- Generate and review the migration.
- Add tests for the uniqueness constraint and persistence round trip.

**Checkpoint:** Canonical fields round-trip correctly and duplicate canonical
keys are rejected.

### 5. Build brand definitions from the database

- Add a service that loads `Brand` records and converts them to
  `BrandDefinition` values.
- Use `Brand.Slug` as the stable key and `Brand.Name` as the display name.
- Merge aliases from `Canonicalization:BrandAliases` by matching `BrandKey` to
  `Brand.Slug`.
- Treat an alias entry that references an unknown brand slug as a configuration
  error.
- Test a brand without aliases, a brand with configured aliases, and an unknown
  configured brand key.

**Checkpoint:** Normalization receives one deterministic set of database-backed
brand definitions.

### 6. Introduce explicit canonicalization outcomes

- Create a result type with `Matched`, `Created`, and `Unresolved` outcomes.
- Include `StoreProductId`, the resulting `ProductId` when one exists, and a
  machine-readable failure reason.
- Reuse existing normalization failure reasons where possible.
- Add distinct reasons for a missing brand record, conflicting canonical
  products, and persistence failure.
- Add unit tests for the invariants of every outcome.

**Checkpoint:** Callers can handle every result without inferring state from
null values or exception text.

### 7. Implement one-listing canonicalization

- Add a retailer-neutral `IStoreProductCanonicalizer`.
- Give it one operation that accepts a persisted, unlinked `StoreProduct`, its
  category, the available brand definitions, and a cancellation token.
- Normalize `StoreProductName` with `IProductNormalizationService`.
- Build the key with `ICanonicalKeyBuilder`.
- Look up `Product` by exact canonical key.
- If exactly one product exists, assign its ID and return `Matched`.
- If none exists and normalization is complete, create a canonical `Product`,
  link the listing, and return `Created`.
- If normalization fails or stored data conflicts, leave `ProductId` null and
  return `Unresolved`.
- Never use fuzzy similarity to create or link a product.

**Checkpoint:** Unit and persistence tests cover exact match, creation, and each
unresolved path.

### 8. Map normalized values into a new canonical product

- Set `Name` from the trimmed source title for the initial catalog seed.
- Set `NormalizedName`, canonical quantity, unit, variant, package type,
  category, brand, and canonical key from the normalized result.
- Copy the listing image only when creating the product.
- Use one injected clock value for all timestamps in the operation.
- Do not copy retailer prices, stock, URLs, SKUs, or retailer names.
- Test every mapped field.

**Checkpoint:** A created product contains only canonical attributes and allowed
seed metadata.

### 9. Handle canonical-key races safely

- Keep the unique database index as the final duplicate guard.
- If a concurrent insert loses the canonical-key race, detach or clear the
  failed tracked insert, reload the product by canonical key, and link the
  listing to it.
- Only recover from the provider-specific unique violation for the canonical-key
  index; do not swallow unrelated database exceptions.
- Add a PostgreSQL integration test proving that concurrent attempts cannot
  create duplicate canonical products.

**Checkpoint:** Both competing listings link successfully to one product.

### 10. Add batch canonicalization

- Add `IStoreProductCanonicalizationService`.
- Query only listings that require work: initially, those whose `ProductId` is
  null.
- Order them deterministically by `StoreId` and then `ExternalProductId`.
- Process bounded batches rather than loading the entire catalog.
- Save after each batch so a later failure does not discard completed batches.
- Return counts for matched, created, unresolved, and failed listings.
- Honor cancellation before starting each listing and each batch.

**Checkpoint:** A batch run is deterministic, bounded, resumable, and returns an
accurate summary.

### 11. Keep import and canonicalization in separate transactions

- Introduce a Shwapno import orchestration service.
- Run and commit the JSON-to-`StoreProduct` import first.
- Start batch canonicalization only after that commit succeeds.
- Do not start canonicalization if import fails.
- Preserve successfully imported listings when canonicalization fails, and
  report that failure separately.
- Ensure a later run retries every listing that still requires canonicalization.

**Checkpoint:** A forced canonicalization failure does not roll back the import.

### 12. Trigger the orchestrator during application startup

- Replace the direct `ShwapnoDairyImporter.ImportAsync` call in database
  initialization with the orchestration service.
- Pass the application cancellation token through every layer.
- Log separate import and canonicalization summaries.
- Ensure migrations finish before either phase begins.

**Checkpoint:** Startup logs clearly show the two phases and their independent
outcomes.

### 13. Correct price-history idempotency

- Compare incoming price, original price, and stock status with the persisted
  listing before updating it.
- Add `PriceHistory` only if at least one of those values changes.
- Preserve the initial history record for a newly imported listing.
- Test a repeated unchanged import and changes to each tracked offer field.

**Checkpoint:** Reimporting unchanged JSON adds no history, while each real offer
change adds exactly one row.

### 14. Track when a linked listing must be reprocessed

- Add a source-identity fingerprint derived from the normalized source title and
  category key.
- Store the fingerprint and a canonicalization version on `StoreProduct`.
- Skip a linked listing when its fingerprint and version still match.
- Clear and recompute its canonical link when the identity fingerprint changes.
- Increment the canonicalization version when parsing or key rules change and a
  full reprocessing run is required.
- Do not reprocess for price, stock, URL, or image-only changes.
- Generate and review the required migration.

**Checkpoint:** Identity changes trigger relinking, while offer-only changes do
not.

### 15. Report unresolved listings

- Generate a structured JSON report after each batch run.
- Include store slug, external SKU, original title, failure code, extracted
  attributes when available, and timestamp.
- Write to a temporary file in the destination directory and atomically replace
  the final report only after serialization succeeds.
- Do not include fuzzy suggestions in the first implementation.
- Keep unresolved listings in the database with a null `ProductId`.

**Checkpoint:** Every unresolved listing has an actionable report entry, and an
interrupted write cannot corrupt the previous report.

### 16. Add end-to-end integration coverage

- Create a small fixture containing two equivalent listings, one new valid
  product, one unknown brand, and one ambiguous quantity.
- Assert that the equivalent listings share one canonical product.
- Assert that the new valid listing creates one product.
- Assert that unresolved listings remain stored but unlinked and appear in the
  report.
- Run the fixture twice and assert there are no duplicate listings, products,
  links, or price-history rows.
- Simulate canonicalization failure and verify the imported listings remain
  committed.

**Checkpoint:** The end-to-end tests demonstrate the complete transaction and
idempotency guarantees.

### 17. Clean up and document operation

- Remove obsolete mapper code and unused imports.
- Document how to run an import, interpret its summary, inspect unresolved
  entries, and retry canonicalization.
- Explain when to add a brand alias and when to modify a parsing rule.
- Run the complete test suite.
- Resolve any new warnings introduced by this work. Record unrelated baseline
  warnings separately rather than hiding them.

**Checkpoint:** The operational instructions are usable, the full suite passes,
and the change introduces no new warnings.

## Public interfaces and types

- `StoreProduct.ProductId` becomes nullable.
- `Product` gains `CanonicalKey`, `Variant`, and `PackageType`.
- `IStoreProductCanonicalizer` owns one-listing behavior.
- `IStoreProductCanonicalizationService` owns bounded batch processing.
- A Shwapno orchestration service returns separate import and canonicalization
  summaries.
- Existing product HTTP response shapes remain unchanged.

## Definition of done

- Importing JSON creates or updates retailer listings without requiring
  canonical products.
- Canonicalization starts only after the import transaction commits.
- Valid listings deterministically link to an existing or newly created
  canonical product.
- Uncertain listings remain stored and unlinked with an actionable reason.
- Repeated and concurrent runs cannot create duplicate canonical products.
- Canonicalization failures never erase a successful import.
- Every implementation step has focused tests, and the complete suite passes.

## Assumptions

- Shwapno dairy is the initial catalog seed.
- Exact canonical-key matching is the only automatic matching strategy.
- PostgreSQL is the integration-test target for relational and concurrency
  behavior.
- Each numbered step is completed and verified before beginning the next one.
