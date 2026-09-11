using System;
using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Canonicalization.Products;
using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace CartCompareAPI.Canonicalization.StoreProducts;

public class StoreProductCanonicalizer
    (AppDbContext db,
    IProductNormalizationService normalizationService,
    ICanonicalKeyBuilder canonicalKeyBuilder)
    : IStoreProductCanonicalizer
{
    public async Task<StoreProductCanonicalizationResult> CanonicalizeAsync(
        StoreProduct storeProduct,
        Category category,
        IReadOnlyCollection<BrandDefinition> brandDefinitions,
        CancellationToken cancellationToken = default
    )
    {
        ProductNormalizationResult normalizationResult = normalizationService.Normalize(
            storeProduct.StoreProductName,
            brandDefinitions
        );

        if (!normalizationResult.IsSuccess)
        {
            StoreProductCanonicalizationFailure failure =
                normalizationResult.Failure!.Value.ToStoreProductFailure();

            StoreProductCanonicalizationResult unresolvedResult =
                StoreProductCanonicalizationResult.Unresolved(storeProduct.Id, failure);

            return unresolvedResult;
        }

        NormalizedProduct normalizedProduct = normalizationResult.Product!;
        string canonicalKey = canonicalKeyBuilder.Build(
            category.Slug,
            normalizedProduct,
            includePackageDisambiguator: true);


        Product? existingProduct = await db.Products
            .SingleOrDefaultAsync(
                p => p.CanonicalKey == canonicalKey,
                cancellationToken
        );

        if (existingProduct is not null)
        {
            storeProduct.ProductId = existingProduct.Id;
            storeProduct.Product = existingProduct;

            return StoreProductCanonicalizationResult.Matched(
                storeProduct.Id,
                existingProduct.Id);
        }

        throw new NotImplementedException(
            $"Canonical product lookup is not implemented for key '{canonicalKey}'.");
    }
}
