
using CartCompareAPI.Infrastructure.Data;
using CartCompareAPI.Infrastructure.Seed;

namespace CartCompareApi.Infrastructure.Data.Seed;

public static class DatabaseSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Categories.Any())
            return;

        var categories = CategorySeeder.Seed(db);

        var brands = BrandSeeder.Seed(db);

        var stores = StoreSeeder.Seed(db);

        var products = ProductSeeder.Seed(
            db,
            categories,
            brands
        );

        var storeProducts = StoreProductSeeder.Seed(
            db,
            stores,
            products
        );

        PriceHistorySeeder.Seed(
            db,
            storeProducts
        );
    }
}