using CartCompareAPI.Canonicalization.Brands;
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


    }
}
