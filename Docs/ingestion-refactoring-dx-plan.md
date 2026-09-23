# Ingestion Refactoring and Developer Experience Plan

## Goal

Make the complete ingestion flow easy to find, follow, test, and extend before
adding the scheduled background worker. Preserve current behavior while the code
is reorganized.

After the refactor, a developer should be able to start at one endpoint or
worker entry point and follow this path without searching the whole solution:

```text
trigger
  -> ingestion workflow
  -> Shwapno product source
  -> Shwapno catalog import
  -> store-product canonicalization
  -> canonicalization issue tracking
  -> run result
```

## Current sources of confusion

The current code works, but its ownership and names do not consistently reveal
the flow.

- `AddShwapnoIngestion` is in `Features/Products/ProductsModule.cs`, even though
  it registers the ingestion feature.
- `ShwapnoController.cs` is physically under `Controllers` but declares the
  `CartCompareAPI.Ingestion.Shwapno.Browser` namespace.
- `ShwapnoDairyImporter` imports every supported category, so its name describes
  behavior that no longer exists.
- `ShwapnoBrowserClient` is really a Playwright-backed product source. Client
  does not say what it returns or distinguish it from an HTTP client.
- The main workflow types, API filters, errors, and result contracts share one
  folder and repeat the full `ShwapnoIngestion` prefix.
- Shwapno response models are spread over many very small files under a folder
  named `Entities`, which makes them look like domain or database entities.
- Canonicalization has both `StoreProductCanonicalizationService` and
  `StoreProductCanonicalizer`. Their names do not explain that one handles a
  batch while the other handles one listing.
- Canonicalization issue persistence is hidden inside the batch service.
- `ShwapnoJsonReader` remains beside the live import path even though live
  ingestion now passes products in memory.
- Shwapno services are registered in multiple places, including duplicate
  registrations for `ShwapnoCatalogInitializer` and `ShwapnoProductMapper`.
- Tests mostly use a flatter structure than production, so production and test
  files are harder to compare side by side.

## Step 1 baseline (2026-09-24)

**Status:** Completed.

### Verification

- Full solution test run: **160 passed, 0 failed, 0 skipped**.
- Command:

```powershell
dotnet test CartCompareAPI.slnx --nologo --no-restore
```

- Existing build warning: the test project resolves conflicting
  `Microsoft.EntityFrameworkCore.Relational` versions 10.0.4 and 10.0.11.
- Existing restore warning: NuGet vulnerability metadata could not be reached
  in the restricted environment.
- Repository migration tip:
  `20260923220047_AddNamesToCanonicalizationIssues`.

### Current workflow map

Follow the live request in this order:

1. Trigger:
   [`ShwapnoController`](../CartCompareAPI/Ingestion/Shwapno/Controllers/ShwapnoController.cs)
2. Complete workflow:
   [`ShwapnoIngestionOrchestrator`](../CartCompareAPI/Ingestion/Shwapno/ShwapnoIngestionOrchestrator.cs)
3. Playwright product source:
   [`ShwapnoBrowserClient`](../CartCompareAPI/Ingestion/Shwapno/Browser/ShwapnoBrowserClient.cs)
4. Import and commit:
   [`ShwapnoDairyImporter`](../CartCompareAPI/Ingestion/Shwapno/Import/ShwapnoDairyImporter.cs)
5. Retailer field mapping:
   [`ShwapnoProductMapper`](../CartCompareAPI/Ingestion/Shwapno/Import/ShwapnoProductMapper.cs)
6. Pending-listing batch:
   [`StoreProductCanonicalizationService`](../CartCompareAPI/Canonicalization/StoreProducts/StoreProductCanonicalizationService.cs)
7. One-listing match or creation:
   [`StoreProductCanonicalizer`](../CartCompareAPI/Canonicalization/StoreProducts/StoreProductCanonicalizer.cs)
8. Persisted unresolved item:
   [`StoreProductCanonicalizationIssue`](../CartCompareAPI/Domain/Entities/StoreProductCanonicalizationIssue.cs)
9. Returned phase summaries:
   [`ShwapnoIngestionResult`](../CartCompareAPI/Ingestion/Shwapno/ShwapnoIngestionResult.cs)

The import transaction commits before canonicalization starts. A
canonicalization exception therefore returns partial success and does not undo
the imported `StoreProduct` rows.

### Recorded behavior

A manually observed `loose-rice` run completed the scrape and import phases with
this result:

```json
{
  "status": "Completed",
  "scrape": { "productsCollected": 8 },
  "import": { "received": 8, "created": 8, "updated": 0 },
  "canonicalization": {
    "succeeded": true,
    "summary": {
      "matched": 0,
      "created": 0,
      "unresolved": 8,
      "failed": 0
    },
    "error": null
  }
}
```

This distinguishes a completed ingestion run from complete canonicalization:
the workflow succeeded, but eight listings required later resolution.

A current tested unresolved example is a product title without a reliable
quantity, such as `Milk Powder large pack`. Normalization returns
`QuantityNotResolved`, the listing remains unlinked, and the batch records or
updates its row in `StoreProductCanonicalizationIssues` with its store, source
product, reason, timestamps, and attempt count.

### Baseline invariants to preserve

- The endpoint remains `POST /api/v1/ingestion/shwapno?category={slug}`.
- Scraping returns products in memory; the live flow does not require JSON.
- Import commits before canonicalization.
- Canonicalization failures are reported separately from scrape/import
  failures.
- Pending listings remain retryable.
- Unresolved and failed listings are recorded for later inspection.
- The ingestion API key and in-process concurrency filters run before the
  controller action.

## Design rules for the refactor

1. Organize by feature and workflow phase. A folder should answer where am I in
   the flow?
2. Let the containing folder and namespace provide context. Avoid repeating
   `ShwapnoIngestion` in every type when `Ingestion/Shwapno` already says it.
3. Use role-specific implementation names. Prefer `BatchCanonicalizer` and
   `ListingCanonicalizer` over two nearly identical service names.
4. Keep an interface only when it is a real boundary: an external adapter, a
   test seam, or a likely alternate implementation.
5. Keep closely related records and enums together when they always change
   together. One type per file is a convention, not a requirement.
6. Keep dependency registration inside the feature that owns the services.
7. Make namespaces match folders.
8. Complete moves and renames separately from behavior changes so reviews and
   failures are easy to understand.

## Proposed production layout

This is the target direction, not a requirement to move every file in one
commit.

```text
CartCompareAPI/
  Features/
    Ingestion/
      Shwapno/
        ShwapnoIngestionModule.cs
        Api/
          ShwapnoIngestionController.cs
          IngestionApiKeyFilter.cs
          IngestionConcurrencyFilter.cs
          IngestionExceptionHandler.cs
        Workflow/
          IShwapnoIngestionWorkflow.cs
          ShwapnoIngestionWorkflow.cs
          IngestionRunResult.cs
          IngestionExceptions.cs
        Scraping/
          IShwapnoProductSource.cs
          PlaywrightShwapnoProductSource.cs
          ShwapnoApiModels.cs
        Importing/
          IShwapnoCatalogImporter.cs
          ShwapnoCatalogImporter.cs
          ShwapnoProductMapper.cs
          ShwapnoCatalogResolver.cs
          ImportResult.cs

    Catalog/
      Canonicalization/
        CanonicalizationModule.cs
        Batch/
          IStoreProductBatchCanonicalizer.cs
          StoreProductBatchCanonicalizer.cs
          BatchCanonicalizationResult.cs
        Listing/
          IStoreProductCanonicalizer.cs
          StoreProductCanonicalizer.cs
          ListingCanonicalizationResult.cs
        Issues/
          CanonicalizationIssueRecorder.cs
        Normalization/
          ProductNormalizer.cs
          CanonicalKeyBuilder.cs
          Brands/
          Names/
          Packaging/
          Quantities/
          Variants/
```

The domain entities and `AppDbContext` remain in their current domain and
infrastructure locations. Migrations also remain under `Migrations`.

## Naming map

Use this map to remove the most confusing collisions first.

| Current name | Proposed name | Reason |
|---|---|---|
| `ShwapnoDairyImporter` | `ShwapnoCatalogImporter` | It imports all Shwapno categories. |
| `ShwapnoBrowserClient` | `PlaywrightShwapnoProductSource` | Names the technology and responsibility. |
| `ShwapnoIngestionOrchestrator` | `ShwapnoIngestionWorkflow` | Workflow describes the ordered application use case. |
| `IShwapnoIngestionOrchestrator` | `IShwapnoIngestionWorkflow` | Matches the implementation and entry-point role. |
| `ShwapnoCatalogInitializer` | `ShwapnoCatalogResolver` | It finds or creates the store/category needed by an import. |
| `StoreProductCanonicalizationService` | `StoreProductBatchCanonicalizer` | It selects and processes a batch of pending listings. |
| `IStoreProductCanonicalizationService` | `IStoreProductBatchCanonicalizer` | Makes the batch boundary visible at call sites. |
| `StoreProductCanonicalizer` | keep | It handles exactly one store listing. |
| `ShwapnoIngestionResult` | `IngestionRunResult` | Shorter inside the Shwapno workflow namespace. |
| `ShwapnoImportResult` | `ImportResult` | Shorter inside the importing namespace. |

Do not rename database tables or columns just to match these application type
names. Those changes add migration risk without improving navigation.

## Refactoring steps

### 1. Record the baseline and workflow map

**Status:** Completed on 2026-09-24. See
[Step 1 baseline](#step-1-baseline-2026-09-24).

- Run the full test suite and record the test count.
- Record one successful ingestion response and one unresolved-product example.
- Add a short workflow section to the repository README or this document with
  links to the trigger, workflow, source, importer, batch canonicalizer, and
  issue entity.
- Record the current database migration at the tip.

**Checkpoint:** The existing behavior can be compared after every later step.

### 2. Give ingestion one composition root

- Move `AddShwapnoIngestion` out of `ProductsModule` and into a new
  `ShwapnoIngestionModule` owned by the ingestion feature.
- Register every Shwapno service in that module exactly once.
- Remove the direct `ShwapnoBrowserClient` registration from `Program.cs`.
- Remove Shwapno registrations from `AddProductFeatures` and
  `AddInfrastructure`.
- Keep `Program.cs` at the module level:

```csharp
builder.Services.AddShwapnoIngestion(builder.Configuration);
```

**Checkpoint:** There is one file to inspect to learn all Shwapno dependencies,
and dependency-injection resolution tests still pass.

### 3. Fix folder and namespace mismatches

- Move the controller into the ingestion API folder.
- Change its namespace so it matches its folder.
- Move filters and the ingestion exception handler beside the API boundary.
- Move orchestration results and exceptions beside the workflow.
- Mirror the same folders in `CartCompareAPI.Tests`.
- Use IDE-safe moves or update namespaces and imports in one focused commit.

**Checkpoint:** Searching for `Shwapno` shows coherent feature folders, and
production files have a predictable matching test location.

### 4. Rename misleading and colliding types

- Apply the naming map above, beginning with `ShwapnoDairyImporter`.
- Rename one responsibility group per commit: source, import, workflow, then
  canonicalization batch.
- Update test class names at the same time as production types.
- Avoid compatibility wrapper types unless another deployed project consumes
  these classes as a library.

**Checkpoint:** A call site makes the sequence readable without opening each
implementation:

```csharp
products = await productSource.GetProductsAsync(...);
import = await catalogImporter.ImportAsync(...);
canonicalization = await batchCanonicalizer.CanonicalizePendingAsync(...);
```

### 5. Replace the `Entities` transport-model folder

- Rename the Shwapno `Entities` folder to `Contracts` or `ApiModels` so these
  types cannot be confused with EF domain entities.
- Combine the small response-only types into `ShwapnoApiModels.cs` if they are
  never used independently: response envelope, product, price, picture, image
  URLs, ribbon, and UOM option.
- Keep `ShwapnoProduct` public/internal as required by the source-to-import
  boundary. Make incidental nested response types internal when possible.
- Preserve JSON property behavior and add one representative deserialization
  test using a small loose-rice response.

**Checkpoint:** One file explains the external Shwapno response shape and the
  loose-rice UOM fields still deserialize correctly.

### 6. Remove obsolete paths and sample leftovers

- Confirm `ShwapnoJsonReader` has no supported runtime caller, then delete it.
- Keep JSON samples only under the test project as intentionally named
  fixtures.
- Remove the default weather controller/model if they are unused.
- Delete unused `using` directives and stale comments while touching each area.
- Do not combine this cleanup with functional canonicalization changes.

**Checkpoint:** Every remaining ingestion file participates in the live flow,
an explicit offline tool, or a test.

### 7. Make workflow contracts compact

- Keep the overall ingestion result, status, scrape summary, import summary,
  and canonicalization phase result together in one workflow contract file.
- Keep importer-only internal details in the importing folder.
- Standardize method verbs:
  - `GetProductsAsync` for scraping;
  - `ImportAsync` for persistence;
  - `CanonicalizePendingAsync` for batch canonicalization;
  - `RunAsync` or `IngestAsync` for the complete workflow.
- Prefer `sealed` records for immutable summaries.

**Checkpoint:** Understanding an endpoint response requires opening one contract
file rather than several similarly named result files.

### 8. Clarify canonicalization boundaries

- Rename the batch service so it cannot be confused with the one-listing
  canonicalizer.
- Keep normalization algorithms grouped below `Normalization`; keep batch
  database coordination below `Batch`.
- Extract `CanonicalizationIssueRecorder` only if issue create/update/remove
  logic continues growing. Its API should express `Record` and `Resolve`, while
  the batch canonicalizer retains control of save boundaries.
- Keep normalization parsers as separate classes because each has focused rules
  and unit tests. Reducing file count here would make those rules harder to
  change safely.

**Checkpoint:** The batch class reads like selection and coordination, while the
single-listing class reads like normalize, match, or create.

### 9. Add a small integration-style workflow test

- Keep the existing focused unit tests.
- Add one test that wires the real workflow with controlled source/import/batch
  adapters and proves the phase order and final result contract.
- Add one database-backed test that imports a product, canonicalizes it or
  records an issue, and verifies the final persisted state.
- Do not launch Playwright in regular unit tests.

**Checkpoint:** Folder moves and DI changes can be verified without calling the
real Shwapno site.

### 10. Add navigation documentation at the feature root

- Add a short `README.md` under the Shwapno feature containing:
  - the flow diagram;
  - the purpose of each subfolder;
  - the public entry points;
  - the transaction boundary between import and canonicalization;
  - how unresolved issues are stored and retried;
  - how to run focused tests.
- Keep it short enough to remain current. Link to this refactoring plan and the
  deployment plan instead of copying their full content.

**Checkpoint:** A new developer can identify the first file to open and the
next three calls in under five minutes.

### 11. Prepare the background-job boundary

Begin this only after steps 1 through 10 are complete.

- Make the API endpoint and future worker depend on the same
  `IShwapnoIngestionWorkflow`.
- Keep API authorization and HTTP filters outside the workflow.
- Keep scheduling, category iteration, process exit codes, and job logging in
  the worker project.
- Keep Playwright, import, canonicalization, and issue tracking in reusable
  application services.
- Add the worker as a second composition root rather than calling the API over
  HTTP.

**Checkpoint:** Adding the worker requires a new trigger and hosting setup, not
a copy of ingestion logic.

## Recommended commit sequence

1. Baseline notes and workflow map.
2. Centralize ingestion dependency registration.
3. Move API and workflow files; fix namespaces.
4. Rename the all-category importer and Playwright source.
5. Rename batch canonicalization types.
6. Consolidate Shwapno API response models.
7. Remove obsolete JSON and sample files.
8. Add feature README and integration-style flow test.
9. Start the one-shot background worker.

Each commit should compile and pass tests. Avoid mixing migrations into this
structural refactor unless an actual persisted model changes.

## Refactoring guardrails

- Do not change endpoint routes, status codes, authentication, or response JSON
  during folder and naming changes.
- Do not change canonical-key rules, quantity parsing, brand behavior, or issue
  lifecycle during structural changes.
- Do not regenerate existing EF migrations because namespaces or class names
  move.
- Do not introduce a generic multi-store framework before a second retailer
  provides concrete requirements.
- Do not create an interface for every class. Keep interfaces at workflow,
  external source, importer, and canonicalization boundaries where tests or
  alternate adapters use them.
- Run focused tests after each move and the full suite at every checkpoint.

## Completion criteria

- All Shwapno ingestion registration is owned by one feature module.
- Folder paths and namespaces match.
- No type is still named Dairy while handling every category.
- The Playwright source, importer, workflow, batch canonicalizer, and
  single-listing canonicalizer have distinct names.
- External Shwapno response models are clearly separated from domain entities.
- Obsolete runtime JSON import code is removed or explicitly isolated.
- Production and test folders mirror each other for ingestion.
- A short feature README explains how to navigate the flow.
- Existing API behavior, database behavior, and tests remain stable.
- The future worker can call the workflow directly through dependency
  injection.
