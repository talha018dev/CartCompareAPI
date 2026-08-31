# Shwapno Product Canonicalization Plan

## Summary

Canonicalize Shwapno dairy listings into shared, retailer-independent `Product`
records while preserving retailer-specific listing and offer data in
`StoreProduct`.

A canonical product represents an exact purchasable variant. Its brand, product
type or flavor, net quantity, and normalized unit must match. Matching will be
deterministic and conservative: uncertain matches will not be linked
automatically and will instead be included in a generated review report.

The first implementation targets Shwapno dairy products, but the
canonicalization services should remain retailer-neutral so future importers can
use the same process.

## Data ownership

### `StoreProduct`

`StoreProduct` represents a retailer's listing and its current offer. Keep the
following fields on it:

| Field | Purpose |
| --- | --- |
| `Id` | Internal listing identity. |
| `StoreId` | The retailer that owns the listing. |
| `ProductId` | Link to the canonical product; nullable while a listing is unresolved. |
| `ExternalProductId` | Retailer product identifier, such as the Shwapno SKU. It remains unique together with `StoreId`. |
| `StoreProductName` | The original retailer title, preserved without canonical cleanup. |
| `Price` | Current selling price. |
| `OriginalPrice` | Original or pre-discount price, when supplied. |
| `InStock` | Current retailer availability. |
| `ProductUrl` | Retailer product page. |
| `ImageUrl` | Retailer-provided listing image. |
| `CreatedAt` | When the listing was first seen. |
| `LastSeenAt` | When the listing was last observed in a successful import. This replaces the ambiguous `LastUpdated` name. |
| `IsActive` | Whether the listing still appears in the retailer feed. |

Do not copy the canonical brand, category, product name, parsed size, canonical
unit, or matching key onto `StoreProduct`. These belong to `Product` or are
derived by the canonicalization pipeline.

Do not persist Shwapno presentation fields such as ratings, ribbons, delivery
flags, display order, quick-view flags, or add-to-cart flags unless a product
feature later requires them. The source JSON files remain the raw archive for
data not represented in the domain model.

### Canonical `Product`

`Product` represents the retailer-independent, exact purchasable variant. Keep:

- `Name`: clean, customer-facing name without retailer promotions.
- `NormalizedName`: normalized name without brand, size, packaging noise, or
  retailer-specific wording.
- `CanonicalKey`: deterministic unique identity used for exact matching.
- `BrandId` and `CategoryId`.
- `Quantity` and `Unit`, converted to canonical base units.
- `Variant`: meaningful descriptors such as `full cream`, `chocolate`, or a
  flavor.
- `PackageType`: optional metadata such as `tin`, `foil`, `BIB`, or `poly`.
- `ImageUrl`, `IsActive`, `CreatedAt`, and `UpdatedAt`.

Use the following inspectable canonical-key structure:

```text
category|brand|normalized-name|variant|quantity-unit|package-disambiguator
```

Add a unique database index on `Product.CanonicalKey`.

Size and meaningful variants are always identity-defining. Packaging is included
in identity only when it is necessary to distinguish independently sold SKUs
whose other canonical attributes are identical.

## Normalization rules

### Name and brand

1. Normalize Unicode, convert to lowercase for comparison, trim whitespace, and
   collapse repeated whitespace.
2. Normalize punctuation and harmless formatting differences such as `2in1`
   versus `2 in 1`.
3. Resolve brands using known brand names and explicit aliases. Brand casing such
   as `MARKS` and `Marks` must resolve to one `Brand`.
4. Remove the resolved brand, net quantity, package text, and retailer promotion
   text before producing `NormalizedName`.
5. Retain descriptors that change the product, including flavor, fat/content
   type, formula, or product line.
6. Do not use `SeName` as the source of product identity. It may be stale or
   inconsistent with the listing name.

### Quantity and unit

Parse net quantity from the product title. The current dairy data reports
`OrderPackageQuantity = 1` and `Unit = "Piece"` for every listing, so those fields
do not describe the actual product size.

Normalize equivalent quantities into base units:

- `1kg`, `1 kg`, `1000gm`, and `1000 g` become `1000 g`.
- `1L`, `1 L`, `1000ml`, and `1000 ml` become `1000 ml`.
- Countable products use `count`.
- Recognize common aliases including `gm`, `g`, `kg`, `ml`, `l`, `pcs`, and
  `piece`.

Keep decimal parsing culture-independent. A title with no reliable quantity, or
with multiple quantities that cannot be distinguished from a promotion, is
unresolved rather than guessed.

### Variant and packaging

Variants such as `full cream`, `chocolate`, or flavor names remain part of the
canonical identity. Different sizes and different meaningful variants must never
match.

Package descriptions are retained as normalized metadata. They affect the
canonical key only when two separately sold SKUs would otherwise have the same
key. Promotional packaging text and retailer-only wording are discarded.

## Canonicalization and import flow

For each source listing:

1. Validate the source SKU, name, and current offer data.
2. Upsert `StoreProduct` using `(StoreId, ExternalProductId)` while preserving the
   source title, URL, and image.
3. Normalize the name, resolve the brand, parse the quantity and unit, and build
   a canonical product candidate.
4. Apply matching in this order:
   1. An explicit SKU-to-product override from a curated JSON mapping file.
   2. An exact `CanonicalKey` match.
   3. A conservative similarity search used only to populate report suggestions.
5. Link the listing when an override or exactly one canonical key matches.
6. During the initial Shwapno catalog seed, create a canonical `Product` when all
   required attributes parse successfully and no match exists.
7. If parsing fails or the result is ambiguous, leave `ProductId` null and add the
   listing to `canonicalization-report.json`. Never automatically select the
   highest-scoring fuzzy candidate.
8. On later imports of the same SKU, update retailer and offer fields only.
   Retailer imports must not rewrite an established canonical product.
9. Add a `PriceHistory` entry only when `Price`, `OriginalPrice`, or `InStock`
   changes.
10. After a complete successful import, mark previously seen listings that are
    absent from the input as inactive. A partial or failed import must not
    deactivate listings.

The report must contain the store, external SKU, original title, extracted
attributes, reason it was not linked, and suggested canonical products with their
scores and matching reasons. Manual decisions are placed in a version-controlled
override JSON file and applied on the next import. The first version will not add
an administration API or review UI.

The importer must remain idempotent: importing unchanged JSON repeatedly cannot
create duplicate products, store listings, or price-history records.

## Implementation phases

### 1. Canonicalization components

- Add retailer-neutral name normalization, brand resolution, quantity parsing,
  candidate building, and deterministic matching services.
- Keep Shwapno DTO mapping separate from canonical product matching.
- Introduce typed results for matched, newly created, and unresolved listings so
  import behavior is explicit.
- Add configuration-backed brand aliases and SKU overrides.

### 2. Schema and migration

- Add `CanonicalKey`, `Variant`, and `PackageType` to `Product` and create the
  unique canonical-key index.
- Make `StoreProduct.ProductId` nullable.
- Add `StoreProduct.IsActive` and rename `LastUpdated` to `LastSeenAt`.
- Preserve the existing unique `(StoreId, ExternalProductId)` constraint.
- Use an additive EF Core migration; do not rebuild or discard the database.

### 3. Existing-data backfill

- Reprocess existing `StoreProductName` values through the same production
  canonicalization pipeline.
- Reassign listings to existing or newly created canonical products.
- Consolidate duplicate canonical records transactionally.
- Delete an obsolete product only after confirming that no store listings refer
  to it.
- Produce the same uncertainty report for records that cannot be safely linked.
- Make the backfill idempotent and safe to resume after failure.

### 4. Query behavior

- Keep existing product API response shapes unchanged during this work.
- Return canonical products rather than retailer listing names.
- Exclude unresolved listings from product comparisons.
- Show products backed by at least one active, linked retailer listing.

## Test plan

### Unit normalization

- `MARKS Full Cream Milk Powder 1kg (TIN)` resolves to brand `Marks`, quantity
  `1000`, unit `g`, and package type `tin`.
- `1kg`, `1 Kg`, `1000gm`, and `1000 g` normalize to the same quantity and unit.
- `400gm` and `1kg` produce different canonical identities.
- Chocolate and plain/full-cream products produce different identities.
- `MARKS`, `Marks`, and configured aliases resolve to the same brand.
- A misleading `SeName` does not override the source SKU or title.
- Promotional quantities are not mistaken for net product quantity.

### Matching

- An exact canonical key links to one existing product.
- An explicit SKU override takes precedence over computed matching.
- An uncertain or conflicting match remains unlinked and appears in the report.
- Similarity scores never cause an automatic link.
- Package type disambiguates otherwise identical independently sold SKUs.

### Import integration

- Importing identical JSON twice creates no duplicate products or listings.
- An unchanged offer creates no additional price-history entry.
- A changed price, original price, or stock status creates one history entry.
- Existing source names, URLs, images, and SKUs remain unchanged during backfill.
- A missing listing becomes inactive only after a complete successful import.
- A failed import does not deactivate existing listings.
- All database updates for one import remain transactional.

## Acceptance criteria

- Every successfully parsed Shwapno dairy listing links to exactly one canonical
  product.
- Equivalent unit expressions resolve to the same canonical quantity.
- Different sizes or meaningful variants cannot be merged automatically.
- No uncertain match is automatically accepted.
- Every unresolved listing is present in the generated report with an actionable
  reason.
- Re-importing unchanged data is idempotent.
- Canonical products contain no retailer-specific pricing, stock, URL, or source
  title data.
- Store listings retain their original retailer identity and current offer data.

## Assumptions

- Shwapno dairy is the initial canonical catalog seed.
- A canonical product is an exact purchasable variant, not a broad product family.
- Brand, product type or flavor, net quantity, and unit define identity.
- Packaging defines identity only when required to separate independently sold
  SKUs.
- Matching is deterministic and conservative; fuzzy matching is report-only.
- Raw Shwapno JSON remains the archive for source fields that are not persisted.
