using CartCompareApi.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;

namespace CartCompareApi.Infrastructure.Data.Seed;

public static class PriceHistorySeeder
{
    public static void Seed(
        AppDbContext db,
        List<StoreProduct> storeProducts)
    {
        var history = new List<PriceHistory>();

        foreach (var storeProduct in storeProducts)
        {
            history.AddRange(
            [
                new PriceHistory
                {
                    StoreProductId = storeProduct.Id,
                    Price = storeProduct.Price + 5,
                    OriginalPrice = null,
                    InStock = true,
                    RecordedAt = DateTime.UtcNow.AddDays(-14)
                },
                new PriceHistory
                {
                    StoreProductId = storeProduct.Id,
                    Price = storeProduct.Price,
                    OriginalPrice = storeProduct.OriginalPrice.HasValue
                        ? storeProduct.Price
                        : null,
                    InStock = storeProduct.InStock,
                    RecordedAt = DateTime.UtcNow
                }
            ]);
        }

        db.PriceHistory.AddRange(history);
        db.SaveChanges();
    }
}