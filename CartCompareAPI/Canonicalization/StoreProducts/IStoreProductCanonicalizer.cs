using System;
using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Domain.Entities;

namespace CartCompareAPI.Canonicalization.StoreProducts;

public interface IStoreProductCanonicalizer
{
    Task<StoreProductCanonicalizationResult> CanonicalizeAsync(
        StoreProduct storeProduct,
        Category category,
        IReadOnlyCollection<BrandDefinition> brandDefinitions,
        CancellationToken cancellationToken = default
    );
}
