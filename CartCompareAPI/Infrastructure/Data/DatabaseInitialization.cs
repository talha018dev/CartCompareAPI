using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Canonicalization.StoreProducts;
using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Ingestion.Shwapno;
using CartCompareAPI.Ingestion.Shwapno.Entities;
using CartCompareAPI.Ingestion.Shwapno.Import;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Infrastructure.Data;

public static class DatabaseInitialization
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        BrandCatalogInitializer brandCatalogInitializer =
            scope.ServiceProvider.GetRequiredService<BrandCatalogInitializer>();

        await brandCatalogInitializer.InitializeAsync();

        ShwapnoDairyImporter importer =
            scope.ServiceProvider.GetRequiredService<ShwapnoDairyImporter>();

        ShwapnoJsonReader jsonReader = scope.ServiceProvider.GetRequiredService<ShwapnoJsonReader>();
        List<ShwapnoProduct> sourceProducts = await jsonReader.ReadProductsAsync();
        await importer.ImportAsync("dairy", sourceProducts);

        IBrandDefinitionProvider brandDefinitionProvider =
            scope.ServiceProvider.GetRequiredService<IBrandDefinitionProvider>();

        IStoreProductCanonicalizer canonicalizer =
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
