using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Canonicalization.StoreProducts;
using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Tests.Canonicalization.StoreProducts;

public sealed class StoreProductCanonicalizationServiceTests
{
    [Fact]
    public async Task CanonicalizePendingAsync_ShouldSelectOnlyPendingListingsForStoreAndCategory()
    {
        await using var db = CreateContext();
        var storeId = Guid.NewGuid();
        var otherStoreId = Guid.NewGuid();
        var category = new Category { Id = Guid.NewGuid(), Name = "Dairy", Slug = "dairy" };
        var otherCategory = new Category
        {
            Id = Guid.NewGuid(), Name = "Fresh Fruits", Slug = "fresh-fruits"
        };
        var target = Listing(storeId, category.Id, "SKU-A");

        db.Categories.AddRange(category, otherCategory);
        db.StoreProducts.AddRange(
            target,
            Listing(storeId, otherCategory.Id, "SKU-B"),
            Listing(otherStoreId, category.Id, "SKU-C"),
            Listing(storeId, category.Id, "SKU-D", Guid.NewGuid()));
        await db.SaveChangesAsync();

        var canonicalizer = new RecordingCanonicalizer();
        var service = new StoreProductCanonicalizationService(
            db, canonicalizer, new EmptyBrandDefinitionProvider());

        var summary = await service.CanonicalizePendingAsync(storeId, category.Id);

        Assert.Equal(new[] { (target.Id, category.Id) }, canonicalizer.Calls);
        Assert.Equal(new StoreProductCanonicalizationSummary(0, 0, 1, 0), summary);
    }

    [Fact]
    public async Task CanonicalizePendingAsync_ShouldProcessPendingListingsInSkuOrder()
    {
        await using var db = CreateContext();
        var storeId = Guid.NewGuid();
        var category = new Category { Id = Guid.NewGuid(), Name = "Dairy", Slug = "dairy" };
        var later = Listing(storeId, category.Id, "SKU-Z");
        var earlier = Listing(storeId, category.Id, "SKU-A");

        db.Categories.Add(category);
        db.StoreProducts.AddRange(later, earlier);
        await db.SaveChangesAsync();

        var canonicalizer = new RecordingCanonicalizer();
        var service = new StoreProductCanonicalizationService(
            db, canonicalizer, new EmptyBrandDefinitionProvider());

        var summary = await service.CanonicalizePendingAsync(storeId, category.Id);

        Assert.Equal(
            new[] { (earlier.Id, category.Id), (later.Id, category.Id) },
            canonicalizer.Calls);
        Assert.Equal(new StoreProductCanonicalizationSummary(0, 0, 2, 0), summary);
    }

    [Fact]
    public async Task CanonicalizePendingAsync_ShouldPersistCreatedProductBeforeNextListing()
    {
        await using var db = CreateContext();
        var storeId = Guid.NewGuid();
        var category = new Category { Id = Guid.NewGuid(), Name = "Dairy", Slug = "dairy" };
        var first = Listing(storeId, category.Id, "SKU-A");
        var second = Listing(storeId, category.Id, "SKU-B");
        db.Categories.Add(category);
        db.StoreProducts.AddRange(first, second);
        await db.SaveChangesAsync();

        var service = new StoreProductCanonicalizationService(
            db, new DatabaseLookingCanonicalizer(db), new EmptyBrandDefinitionProvider());

        var summary = await service.CanonicalizePendingAsync(storeId, category.Id);

        Assert.Equal(new StoreProductCanonicalizationSummary(1, 1, 0, 0), summary);
        var product = await db.Products.SingleAsync();
        Assert.Equal(product.Id, first.ProductId);
        Assert.Equal(product.Id, second.ProductId);
    }

    private static StoreProduct Listing(
        Guid storeId, Guid categoryId, string sku, Guid? productId = null) => new()
    {
        Id = Guid.NewGuid(),
        StoreId = storeId,
        SourceCategoryId = categoryId,
        ProductId = productId,
        ExternalProductId = sku
    };

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private sealed class RecordingCanonicalizer : IStoreProductCanonicalizer
    {
        public List<(Guid StoreProductId, Guid CategoryId)> Calls { get; } = [];

        public Task<StoreProductCanonicalizationResult> CanonicalizeAsync(
            StoreProduct storeProduct,
            Category category,
            IReadOnlyCollection<BrandDefinition> brandDefinitions,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((storeProduct.Id, category.Id));
            return Task.FromResult(StoreProductCanonicalizationResult.Unresolved(
                storeProduct.Id,
                StoreProductCanonicalizationFailure.MissingName));
        }
    }

    private sealed class DatabaseLookingCanonicalizer(AppDbContext db) : IStoreProductCanonicalizer
    {
        public async Task<StoreProductCanonicalizationResult> CanonicalizeAsync(
            StoreProduct storeProduct,
            Category category,
            IReadOnlyCollection<BrandDefinition> brandDefinitions,
            CancellationToken cancellationToken = default)
        {
            var product = await db.Products.SingleOrDefaultAsync(
                candidate => candidate.CanonicalKey == "shared-product",
                cancellationToken);

            if (product is not null)
            {
                storeProduct.Product = product;
                storeProduct.ProductId = product.Id;
                return StoreProductCanonicalizationResult.Matched(storeProduct.Id, product.Id);
            }

            product = new Product
            {
                Id = Guid.NewGuid(),
                Category = category,
                CategoryId = category.Id,
                CanonicalKey = "shared-product",
                Name = "Shared Product"
            };
            db.Products.Add(product);
            storeProduct.Product = product;
            storeProduct.ProductId = product.Id;
            return StoreProductCanonicalizationResult.Created(storeProduct.Id, product.Id);
        }
    }

    private sealed class EmptyBrandDefinitionProvider : IBrandDefinitionProvider
    {
        public Task<IReadOnlyCollection<BrandDefinition>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<BrandDefinition>>([]);
    }
}
