using System.Text.Json;
using CartCompareApi.Domain.Entities;
using CartCompareApi.Ingestion.Shwapno.Entities;
using CartCompareAPI.Infrastructure.Data;
using CartCompareAPI.Ingestion.Shwapno.Import;
using Microsoft.EntityFrameworkCore;

namespace CartCompareApi.Ingestion.Shwapno;

public sealed class ShwapnoDairyImporter(AppDbContext db, IWebHostEnvironment environment, ShwapnoCatalogInitializer catalogInitializer)
{

    public async Task ImportAsync(CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(environment.ContentRootPath, "Ingestion", "Shwapno", "Data", "dairy.json");
        var sourceProducts = await ShwapnoJsonReader.ReadProductsAsync(filePath);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var catalog = await catalogInitializer.ShwapnoCatalogInitializedAsync();
        var category = catalog.Category;
        var store = catalog.Store;
        var now = DateTime.UtcNow;

        var existingStoreProducts = await db.StoreProducts
            .Where(x => x.StoreId == store.Id)
            .Include(x => x.Product)
            .ToDictionaryAsync(x => x.ExternalProductId, cancellationToken);

        foreach (var source in sourceProducts)
        {
            var productUrl = $"https://www.shwapno.com/{source.SeName.TrimStart('/')}";
            var imageUrl = source.Picture?.LargeDeviceUrl?.FullSizeImageUrl;
            var inStock = source.Stock.Equals("InStock", StringComparison.OrdinalIgnoreCase)
                          && source.Status.Equals("Available", StringComparison.OrdinalIgnoreCase);

            if (existingStoreProducts.TryGetValue(source.Sku, out var storeProduct))
            {
                storeProduct.StoreProductName = source.Name;
                storeProduct.Price = source.Price.PriceValue;
                storeProduct.OriginalPrice = source.Price.oldPriceValue;
                storeProduct.InStock = inStock;
                storeProduct.ProductUrl = productUrl;
                storeProduct.ImageUrl = imageUrl;
                storeProduct.LastUpdated = now;
                continue;
            }

            var product = new Product
            {
                CategoryId = category.Id,
                Name = source.Name.Trim(),
                NormalizedName = source.Name.Trim().ToLowerInvariant(),
                Quantity = source.OrderPackageQuantity,
                Unit = source.Unit.Trim(),
                ImageUrl = imageUrl,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            storeProduct = new StoreProduct
            {
                StoreId = store.Id,
                Product = product,
                ExternalProductId = source.Sku,
                StoreProductName = source.Name,
                Price = source.Price.PriceValue,
                OriginalPrice = source.Price.oldPriceValue,
                InStock = inStock,
                ProductUrl = productUrl,
                ImageUrl = imageUrl,
                LastUpdated = now,
                CreatedAt = now
            };
            storeProduct.PriceHistory.Add(new PriceHistory
            {
                Price = storeProduct.Price,
                OriginalPrice = storeProduct.OriginalPrice,
                InStock = storeProduct.InStock,
                RecordedAt = now
            });
            db.StoreProducts.Add(storeProduct);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
