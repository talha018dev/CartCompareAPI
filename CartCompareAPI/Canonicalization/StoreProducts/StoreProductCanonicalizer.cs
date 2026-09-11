using System;
using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Canonicalization.Products;
using CartCompareAPI.Canonicalization.Variants;
using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Canonicalization.StoreProducts;

public sealed class StoreProductCanonicalizer(
    AppDbContext db,
    IProductNormalizationService normalizationService,
    ICanonicalKeyBuilder canonicalKeyBuilder,
    TimeProvider timeProvider)
    : IStoreProductCanonicalizer
{
    public async Task<StoreProductCanonicalizationResult> CanonicalizeAsync(
        StoreProduct storeProduct,
        Category category,
        IReadOnlyCollection<BrandDefinition> brandDefinitions,
        CancellationToken cancellationToken = default)
    {
        ProductNormalizationResult normalizationResult =
            normalizationService.Normalize(
                storeProduct.StoreProductName,
                brandDefinitions);

        if (!normalizationResult.IsSuccess)
        {
            StoreProductCanonicalizationFailure failure =
                normalizationResult.Failure!.Value.ToStoreProductFailure();

            return StoreProductCanonicalizationResult.Unresolved(
                storeProduct.Id,
                failure);
        }

        NormalizedProduct normalizedProduct = normalizationResult.Product!;
        string canonicalKey = canonicalKeyBuilder.Build(
            category.Slug,
            normalizedProduct,
            includePackageDisambiguator: true);


        Product? existingProduct = await db.Products
            .SingleOrDefaultAsync(
                product => product.CanonicalKey == canonicalKey,
                cancellationToken);

        if (existingProduct is not null)
        {
            Link(storeProduct, existingProduct);

            return StoreProductCanonicalizationResult.Matched(
                storeProduct.Id,
                existingProduct.Id);
        }

        Brand? brand = await db.Brands
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Slug == normalizedProduct.Brand.BrandKey,
                cancellationToken);

        if (brand is null)
        {
            return StoreProductCanonicalizationResult.Unresolved(
                storeProduct.Id,
                StoreProductCanonicalizationFailure.BrandRecordNotFound);
        }

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        Product newProduct = CreateProduct(
            storeProduct,
            category,
            brand,
            normalizedProduct,
            canonicalKey,
            now);

        db.Products.Add(newProduct);
        Link(storeProduct, newProduct);

        return StoreProductCanonicalizationResult.Created(
            storeProduct.Id,
            newProduct.Id);
    }

    private static void Link(StoreProduct storeProduct, Product product)
    {
        storeProduct.ProductId = product.Id;
        storeProduct.Product = product;
    }

    private static Product CreateProduct(
        StoreProduct storeProduct,
        Category category,
        Brand brand,
        NormalizedProduct normalizedProduct,
        string canonicalKey,
        DateTime now)
    {
        return new Product
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
            Variant = FormatVariant(normalizedProduct.Variant),
            PackageType = normalizedProduct.PackageType?.Value,
            ImageUrl = storeProduct.ImageUrl,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static string? FormatVariant(ParsedVariant? variant)
    {
        return variant is null
            ? null
            : string.Join(
                "+",
                variant.Values
                    .Select(value => value.Trim().ToLowerInvariant())
                    .OrderBy(value => value, StringComparer.Ordinal));
    }
}
