using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Canonicalization.Packaging;
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

    [Fact]
    public async Task CanonicalizeAsync_WhenBrandRecordDoesNotExist_ShouldReturnUnresolved()
    {
        await using AppDbContext db = CreateDbContext();
        Category category = CreateCategory();
        NormalizedProduct normalizedProduct = CreateNormalizedProduct();
        var canonicalizer = new StoreProductCanonicalizer(
            db,
            new StubNormalizationService(
                ProductNormalizationResult.Success(normalizedProduct)),
            new CanonicalKeyBuilder(),
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
            StoreProductCanonicalizationOutcome.Unresolved,
            result.Outcome);
        Assert.Equal(storeProduct.Id, result.StoreProductId);
        Assert.Null(result.ProductId);
        Assert.Equal(
            StoreProductCanonicalizationFailure.BrandRecordNotFound,
            result.Failure);
        Assert.Null(storeProduct.ProductId);
        Assert.Null(storeProduct.Product);
        Assert.Empty(db.Products.Local);
    }

    [Fact]
    public async Task CanonicalizeAsync_WhenCanonicalKeyDoesNotExist_ShouldCreateAndLinkProduct()
    {
        await using AppDbContext db = CreateDbContext();
        Category category = CreateCategory();
        var brand = new Brand
        {
            Id = Guid.NewGuid(),
            Name = "Marks",
            Slug = "marks"
        };
        db.Brands.Add(brand);
        await db.SaveChangesAsync();

        NormalizedProduct normalizedProduct = CreateNormalizedProduct();
        var keyBuilder = new CanonicalKeyBuilder();
        string expectedCanonicalKey = keyBuilder.Build(
            category.Slug,
            normalizedProduct,
            includePackageDisambiguator: true);
        var now = new DateTimeOffset(
            2026,
            9,
            11,
            10,
            30,
            0,
            TimeSpan.Zero);
        var canonicalizer = new StoreProductCanonicalizer(
            db,
            new StubNormalizationService(
                ProductNormalizationResult.Success(normalizedProduct)),
            keyBuilder,
            new FixedTimeProvider(now));
        var storeProduct = new StoreProduct
        {
            Id = Guid.NewGuid(),
            StoreProductName = normalizedProduct.SourceName,
            ImageUrl = "https://images.example/marks-milk-powder.jpg"
        };

        StoreProductCanonicalizationResult result =
            await canonicalizer.CanonicalizeAsync(
                storeProduct,
                category,
                [],
                CancellationToken.None);

        Assert.Equal(
            StoreProductCanonicalizationOutcome.Created,
            result.Outcome);
        Assert.Equal(storeProduct.Id, result.StoreProductId);
        Assert.NotNull(result.ProductId);
        Assert.Null(result.Failure);

        Product createdProduct = Assert.Single(db.Products.Local);
        Assert.Equal(result.ProductId, createdProduct.Id);
        Assert.Equal(createdProduct.Id, storeProduct.ProductId);
        Assert.Same(createdProduct, storeProduct.Product);
        Assert.Equal(category.Id, createdProduct.CategoryId);
        Assert.Same(category, createdProduct.Category);
        Assert.Equal(brand.Id, createdProduct.BrandId);
        Assert.Same(brand, createdProduct.Brand);
        Assert.Equal(normalizedProduct.SourceName.Trim(), createdProduct.Name);
        Assert.Equal(normalizedProduct.NormalizedName, createdProduct.NormalizedName);
        Assert.Equal(expectedCanonicalKey, createdProduct.CanonicalKey);
        Assert.Equal(1000m, createdProduct.Quantity);
        Assert.Equal("g", createdProduct.Unit);
        Assert.Equal("chocolate+full cream", createdProduct.Variant);
        Assert.Equal("tin", createdProduct.PackageType);
        Assert.Equal(storeProduct.ImageUrl, createdProduct.ImageUrl);
        Assert.True(createdProduct.IsActive);
        Assert.Equal(now.UtcDateTime, createdProduct.CreatedAt);
        Assert.Equal(now.UtcDateTime, createdProduct.UpdatedAt);
        Assert.Equal(EntityState.Added, db.Entry(createdProduct).State);
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

    private static NormalizedProduct CreateNormalizedProduct()
    {
        return new NormalizedProduct(
            " MARKS Full Cream Chocolate Milk Powder 1kg (TIN) ",
            "full cream chocolate milk powder",
            new BrandResolution("marks", "Marks", "MARKS"),
            new ParsedQuantity(1000m, "g", "1kg"),
            new ParsedPackageType("tin", "TIN"),
            new ParsedVariant(["full cream", "chocolate"]));
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

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
