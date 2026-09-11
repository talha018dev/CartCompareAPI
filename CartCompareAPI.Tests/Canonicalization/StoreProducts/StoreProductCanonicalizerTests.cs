using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Canonicalization.Products;
using CartCompareAPI.Canonicalization.Quantity;
using CartCompareAPI.Canonicalization.StoreProducts;
using CartCompareAPI.Canonicalization.Variants;
using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Tests.Canonicalization.StoreProducts;

public sealed class StoreProductCanonicalizerTests
{
    [Fact]
    public async Task CanonicalizeAsync_WhenNormalizationFails_ShouldReturnUnresolved()
    {
        await using AppDbContext db = CreateDbContext();
        var normalizationService = new StubNormalizationService(
            ProductNormalizationResult.Unresolved(
                ProductNormalizationFailure.MissingName));
        var canonicalizer = new StoreProductCanonicalizer(
            db,
            normalizationService,
            new CanonicalKeyBuilder(),
            TimeProvider.System);
        var storeProduct = new StoreProduct
        {
            Id = Guid.NewGuid(),
            StoreProductName = string.Empty
        };
        Category category = CreateCategory();

        StoreProductCanonicalizationResult result =
            await canonicalizer.CanonicalizeAsync(
                storeProduct,
                category,
                [],
                CancellationToken.None);

        Assert.Equal(
            StoreProductCanonicalizationOutcome.Unresolved,
            result.Outcome);
        Assert.Equal(storeProduct.Id, result.StoreProductId);
        Assert.Null(result.ProductId);
        Assert.Equal(
            StoreProductCanonicalizationFailure.MissingName,
            result.Failure);
        Assert.Null(storeProduct.ProductId);
        Assert.Null(storeProduct.Product);
    }

    [Fact]
    public async Task CanonicalizeAsync_WhenCanonicalKeyExists_ShouldLinkAndReturnMatched()
    {
        await using AppDbContext db = CreateDbContext();
        Category category = CreateCategory();
        var normalizedProduct = new NormalizedProduct(
            "MARKS Full Cream Milk Powder 1kg (TIN)",
            "full cream milk powder",
            new BrandResolution("marks", "Marks", "MARKS"),
            new ParsedQuantity(1000m, "g", "1kg"),
            null,
            new ParsedVariant(["full cream"]));
        var keyBuilder = new CanonicalKeyBuilder();
        string canonicalKey = keyBuilder.Build(
            category.Slug,
            normalizedProduct,
            includePackageDisambiguator: true);
        var existingProduct = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            CanonicalKey = canonicalKey,
            Name = normalizedProduct.SourceName,
            NormalizedName = normalizedProduct.NormalizedName,
            Quantity = normalizedProduct.Quantity.Value,
            Unit = normalizedProduct.Quantity.Unit,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Products.Add(existingProduct);
        await db.SaveChangesAsync();

        var canonicalizer = new StoreProductCanonicalizer(
            db,
            new StubNormalizationService(
                ProductNormalizationResult.Success(normalizedProduct)),
            keyBuilder,
            TimeProvider.System);
        var storeProduct = new StoreProduct
        {
            Id = Guid.NewGuid(),
            StoreProductName = normalizedProduct.SourceName
        };

        StoreProductCanonicalizationResult result =
            await canonicalizer.CanonicalizeAsync(
                storeProduct,
                category,
                [],
                CancellationToken.None);

        Assert.Equal(
            StoreProductCanonicalizationOutcome.Matched,
            result.Outcome);
        Assert.Equal(storeProduct.Id, result.StoreProductId);
        Assert.Equal(existingProduct.Id, result.ProductId);
        Assert.Null(result.Failure);
        Assert.Equal(existingProduct.Id, storeProduct.ProductId);
        Assert.Same(existingProduct, storeProduct.Product);
        Assert.Single(db.Products);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Category CreateCategory()
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = "Dairy",
            Slug = "dairy"
        };
    }

    private sealed class StubNormalizationService(
        ProductNormalizationResult result)
        : IProductNormalizationService
    {
        public ProductNormalizationResult Normalize(
            string productName,
            IReadOnlyCollection<BrandDefinition> brands)
        {
            return result;
        }
    }
}
