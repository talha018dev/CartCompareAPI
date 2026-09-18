using CartCompareAPI.Infrastructure.Data;
using CartCompareAPI.Ingestion.Shwapno;
using CartCompareAPI.Ingestion.Shwapno.Entities;
using CartCompareAPI.Ingestion.Shwapno.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CartCompareAPI.Tests.Ingestion.Shwapno;

public sealed class ShwapnoDairyImporterTests
{
    [Theory]
    [InlineData("dairy")]
    [InlineData("fresh-fruits")]
    public async Task ImportAsync_ShouldPersistUnlinkedListingAndHistoryWithoutProduct(string categorySlug)
    {
        var source = new ShwapnoProduct
        {
            Name = "Fresh Product 500 ml",
            Sku = "SKU-1",
            SeName = "/fresh-product",
            Stock = "InStock",
            Status = "Available",
            Price = new PriceClass
            {
                PriceValue = 95m,
                oldPriceValue = 100m
            },
            Picture = new Picture
            {
                LargeDeviceUrl = new LargeDeviceUrl
                {
                    FullSizeImageUrl = "https://images.example/fresh-product.jpg"
                }
            }
        };

        await using var db = CreateContext();
        var importer = CreateImporter(db);

        var result = await importer.ImportAsync(categorySlug, new[] { source });

        Assert.Equal(new ShwapnoImportSummary(1, 1, 0), result.Summary);

        var category = await db.Categories.SingleAsync();
        var store = await db.Stores.SingleAsync();
        Assert.Equal(categorySlug, category.Slug);
        Assert.Equal(category.Id, result.CategoryId);
        Assert.Equal(store.Id, result.StoreId);

        var listing = await db.StoreProducts
            .Include(product => product.SourceCategory)
            .Include(product => product.PriceHistory)
            .SingleAsync();

        Assert.Null(listing.ProductId);
        Assert.Equal(category.Id, listing.SourceCategoryId);
        Assert.Equal(categorySlug, listing.SourceCategory?.Slug);
        Assert.Single(listing.PriceHistory);
        Assert.Empty(await db.Products.ToListAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("fresh fruits")]
    public async Task ImportAsync_ShouldRejectInvalidCategoryBeforeWriting(string? categorySlug)
    {
        await using var db = CreateContext();
        var importer = CreateImporter(db);
        var products = new[] { ValidProduct() };

        await Assert.ThrowsAsync<UnsupportedShwapnoCategoryException>(() =>
            importer.ImportAsync(categorySlug!, products));

        await AssertNoCatalogWrites(db);
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("missing-sku")]
    [InlineData("missing-sku-after-valid")]
    [InlineData("missing-price")]
    [InlineData("zero-price")]
    public async Task ImportAsync_ShouldRejectInvalidProductsBeforeWriting(string invalidInput)
    {
        await using var db = CreateContext();
        var importer = CreateImporter(db);
        var product = ValidProduct();

        IReadOnlyCollection<ShwapnoProduct> products = invalidInput switch
        {
            "empty" => Array.Empty<ShwapnoProduct>(),
            "missing-sku-after-valid" => new[] { ValidProduct(), product },
            _ => new[] { product }
        };

        if (invalidInput is "missing-sku" or "missing-sku-after-valid") product.Sku = " ";
        if (invalidInput == "missing-price") product.Price = null!;
        if (invalidInput == "zero-price") product.Price.PriceValue = 0;

        await Assert.ThrowsAsync<InvalidShwapnoSourceDataException>(() =>
            importer.ImportAsync("fresh-fruits", products));

        await AssertNoCatalogWrites(db);
    }

    [Fact]
    public async Task ImportAsync_ShouldRejectNullCollectionBeforeWriting()
    {
        await using var db = CreateContext();
        var importer = CreateImporter(db);

        await Assert.ThrowsAsync<InvalidShwapnoSourceDataException>(() =>
            importer.ImportAsync("dairy", null!));

        await AssertNoCatalogWrites(db);
    }

    [Fact]
    public async Task ImportAsync_ShouldNotWriteWhenCancelledBeforeImport()
    {
        await using var db = CreateContext();
        var importer = CreateImporter(db);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            importer.ImportAsync("dairy", new[] { ValidProduct() }, cancellation.Token));

        await AssertNoCatalogWrites(db);
    }

    [Fact]
    public async Task ImportAsync_ShouldImportTwoCategoriesIntoOneShwapnoStore()
    {
        await using var db = CreateContext();
        var importer = CreateImporter(db);
        var dairyProduct = ValidProduct();
        var fruitProduct = ValidProduct();
        fruitProduct.Sku = "SKU-2";

        var dairyResult = await importer.ImportAsync("dairy", new[] { dairyProduct });
        var fruitResult = await importer.ImportAsync("fresh-fruits", new[] { fruitProduct });

        Assert.Equal(
            new[] { "dairy", "fresh-fruits" },
            await db.Categories.OrderBy(category => category.Slug)
                .Select(category => category.Slug).ToArrayAsync());
        Assert.Single(await db.Stores.ToListAsync());
        Assert.Equal(dairyResult.StoreId, fruitResult.StoreId);
        Assert.NotEqual(dairyResult.CategoryId, fruitResult.CategoryId);
        Assert.Equal(2, await db.StoreProducts.CountAsync());
    }

    [Fact]
    public async Task ImportAsync_ShouldKeepFirstSourceCategoryWhenSkuAppearsElsewhere()
    {
        await using var db = CreateContext();
        var importer = CreateImporter(db);

        await importer.ImportAsync("dairy", new[] { ValidProduct() });
        var result = await importer.ImportAsync("fresh-fruits", new[] { ValidProduct() });

        var listing = await db.StoreProducts
            .Include(product => product.SourceCategory)
            .SingleAsync();
        Assert.Equal(new ShwapnoImportSummary(1, 0, 1), result.Summary);
        Assert.Equal("fresh-fruits", (await db.Categories.SingleAsync(
            category => category.Id == result.CategoryId)).Slug);
        Assert.Equal("dairy", listing.SourceCategory?.Slug);
        Assert.Single(await db.StoreProducts.ToListAsync());
    }

    [Fact]
    public async Task ImportAsync_ShouldCountExistingSkuAsUpdated()
    {
        await using var db = CreateContext();
        var importer = CreateImporter(db);
        await importer.ImportAsync("dairy", new[] { ValidProduct() });

        var updatedProduct = ValidProduct();
        updatedProduct.Price.PriceValue = 110m;
        var result = await importer.ImportAsync("dairy", new[] { updatedProduct });

        Assert.Equal(new ShwapnoImportSummary(1, 0, 1), result.Summary);
        var listing = await db.StoreProducts
            .Include(product => product.PriceHistory)
            .SingleAsync();
        Assert.Equal(110m, listing.Price);
        Assert.Equal(2, listing.PriceHistory.Count);
    }

    [Fact]
    public async Task ImportAsync_ShouldCountNewAndExistingSkusInOneBatch()
    {
        await using var db = CreateContext();
        var importer = CreateImporter(db);
        await importer.ImportAsync("fresh-fruits", new[] { ValidProduct() });

        var newProduct = ValidProduct();
        newProduct.Sku = "SKU-2";
        var result = await importer.ImportAsync(
            "fresh-fruits", new[] { ValidProduct(), newProduct });

        Assert.Equal(new ShwapnoImportSummary(2, 1, 1), result.Summary);
        Assert.Equal(2, await db.StoreProducts.CountAsync());
    }

    private static ShwapnoProduct ValidProduct() => new()
    {
        Sku = "SKU-1",
        Price = new PriceClass { PriceValue = 95m }
    };

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(
                InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private static ShwapnoDairyImporter CreateImporter(AppDbContext db) => new(
        db,
        new ShwapnoCatalogInitializer(db),
        new ShwapnoProductMapper());

    private static async Task AssertNoCatalogWrites(AppDbContext db)
    {
        Assert.Empty(await db.Categories.ToListAsync());
        Assert.Empty(await db.Stores.ToListAsync());
        Assert.Empty(await db.StoreProducts.ToListAsync());
        Assert.Empty(await db.PriceHistory.ToListAsync());
    }
}
