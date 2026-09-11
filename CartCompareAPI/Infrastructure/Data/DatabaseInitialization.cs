using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Canonicalization.StoreProducts;
using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Ingestion.Shwapno;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Infrastructure.Data;

public static class DatabaseInitialization
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        var brandCatalogInitializer =
            scope.ServiceProvider.GetRequiredService<BrandCatalogInitializer>();

        await brandCatalogInitializer.InitializeAsync();

        var importer =
            scope.ServiceProvider.GetRequiredService<ShwapnoDairyImporter>();

        await importer.ImportAsync();

        var brandDefinitionProvider =
            scope.ServiceProvider.GetRequiredService<IBrandDefinitionProvider>();

        var canonicalizer =
            scope.ServiceProvider.GetRequiredService<IStoreProductCanonicalizer>();

        Category category = await db.Categories.SingleAsync(
            category => category.Slug == "dairy");

        IReadOnlyCollection<BrandDefinition> brandDefinitions =
            await brandDefinitionProvider.GetAllAsync();

        List<StoreProduct> pendingListings = await db.StoreProducts
            .Where(storeProduct => storeProduct.ProductId == null)
            .OrderBy(storeProduct => storeProduct.ExternalProductId)
            .ToListAsync();

        foreach (StoreProduct storeProduct in pendingListings)
        {
            StoreProductCanonicalizationResult result =
                await canonicalizer.CanonicalizeAsync(
                    storeProduct,
                    category,
                    brandDefinitions);

            await db.SaveChangesAsync();

            Console.WriteLine(
                $"{storeProduct.ExternalProductId}: " +
                $"{result.Outcome}, " +
                $"Failure: {result.Failure}");
        }

    }
}
