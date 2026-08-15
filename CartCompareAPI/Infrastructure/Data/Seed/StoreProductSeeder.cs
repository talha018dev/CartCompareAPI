using CartCompareApi.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;
using static CartCompareApi.Infrastructure.Data.Seed.StoreSeeder;

namespace CartCompareApi.Infrastructure.Data.Seed;

public static class StoreProductSeeder
{
    public static List<StoreProduct> Seed(
        AppDbContext db,
        List<Store> stores,
        List<Product> products)
    {
        var shwapno = stores.First(x => x.Slug == StoreSlugs.Shwapno);
        var agora = stores.First(x => x.Slug == StoreSlugs.Agora);
        var meenaBazar = stores.First(x => x.Slug == StoreSlugs.MeenaBazar);

        var milk = products.First(x => x.NormalizedName == "pran uht milk 1l");
        var crackers = products.First(x => x.NormalizedName == "pran potato crackers 100g");
        var rice = products.First(x => x.NormalizedName == "fresh nazirshail rice 5kg");
        var oil = products.First(x => x.NormalizedName == "radhuni soybean oil 5l");

        var now = DateTime.UtcNow;

        var storeProducts = new List<StoreProduct>
        {

            // Pran Milk
            new()
            {
                StoreId = shwapno.Id,
                ProductId = milk.Id,
                ExternalProductId = "SHP-MILK-1L",
                StoreProductName = "Pran UHT Milk 1L",
                Price = 95,
                InStock = true,
                LastUpdated = now,
                CreatedAt = now
            },
            new()
            {
                StoreId = agora.Id,
                ProductId = milk.Id,
                ExternalProductId = "AGR-MILK-1L",
                StoreProductName = "Pran UHT Milk 1L",
                Price = 98,
                InStock = true,
                LastUpdated = now,
                CreatedAt = now
            },

            // Crackers
            new()
            {
                StoreId = shwapno.Id,
                ProductId = crackers.Id,
                ExternalProductId = "SHP-CRACKER-100",
                StoreProductName = "Pran Potato Crackers 100g",
                Price = 35,
                InStock = true,
                LastUpdated = now,
                CreatedAt = now
            },
            new()
            {
                StoreId = meenaBazar.Id,
                ProductId = crackers.Id,
                ExternalProductId = "MB-CRACKER-100",
                StoreProductName = "Pran Potato Crackers 100g",
                Price = 38,
                InStock = true,
                LastUpdated = now,
                CreatedAt = now
            },

            // Rice
            new()
            {
                StoreId = agora.Id,
                ProductId = rice.Id,
                ExternalProductId = "AGR-RICE-5KG",
                StoreProductName = "Fresh Nazirshail Rice 5kg",
                Price = 450,
                InStock = true,
                LastUpdated = now,
                CreatedAt = now
            },
            new()
            {
                StoreId = meenaBazar.Id,
                ProductId = rice.Id,
                ExternalProductId = "MB-RICE-5KG",
                StoreProductName = "Fresh Nazirshail Rice 5kg",
                Price = 460,
                InStock = true,
                LastUpdated = now,
                CreatedAt = now
            },

            // Oil
            new()
            {
                StoreId = shwapno.Id,
                ProductId = oil.Id,
                ExternalProductId = "SHP-OIL-5L",
                StoreProductName = "Radhuni Soybean Oil 5L",
                Price = 850,
                InStock = true,
                LastUpdated = now,
                CreatedAt = now
            },
            new()
            {
                StoreId = agora.Id,
                ProductId = oil.Id,
                ExternalProductId = "AGR-OIL-5L",
                StoreProductName = "Radhuni Soybean Oil 5L",
                Price = 865,
                InStock = true,
                LastUpdated = now,
                CreatedAt = now
            }
        };

        db.StoreProducts.AddRange(storeProducts);
        db.SaveChanges();

        return storeProducts;
    }
}