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
    ICanonicalKeyBuilder canonicalKeyBuilder,
    TimeProvider timeProvider)
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

        Brand? brand = await db.Brands.SingleOrDefaultAsync(
            b => b.Slug == normalizedProduct.Brand.BrandKey,
            cancellationToken
        );

        if (brand is null)
        {
            return StoreProductCanonicalizationResult.Unresolved(
                storeProduct.Id,
                StoreProductCanonicalizationFailure.BrandRecordNotFound);
        }

        string? variant = normalizedProduct.Variant is null
            ? null
            : string.Join(
                "+",
                normalizedProduct.Variant.Values
                    .Select(value => value.Trim().ToLowerInvariant())
                    .OrderBy(value => value, StringComparer.Ordinal));

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        var newProduct = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Category = category,
            BrandId = brand.Id,
            Brand = brand,
            Name = normalizedProduct.SourceName.Trim(),
            NormalizedName = normalizedProduct.NormalizedName,
            CanonicalKey = canonicalKey,
            Quantity = normalizedProduct.Quantity.Value,
            Unit = normalizedProduct.Quantity.Unit,
            Variant = variant,
            PackageType = normalizedProduct.PackageType?.Value,
            ImageUrl = storeProduct.ImageUrl,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Products.Add(newProduct);
        storeProduct.ProductId = newProduct.Id;
        storeProduct.Product = newProduct;

        return StoreProductCanonicalizationResult.Created(
            storeProduct.Id,
            newProduct.Id);
    }
}
