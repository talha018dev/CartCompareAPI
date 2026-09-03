using System.Text.Json;
using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Ingestion.Shwapno.Entities;
using CartCompareAPI.Infrastructure.Data;
using CartCompareAPI.Ingestion.Shwapno.Import;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Ingestion.Shwapno;

public sealed class ShwapnoDairyImporter(
        AppDbContext db,
        ShwapnoJsonReader jsonReader,
        ShwapnoCatalogInitializer catalogInitializer,
        ShwapnoProductMapper productMapper
    )
{

    public async Task ImportAsync(CancellationToken cancellationToken = default)
    {
        var sourceProducts = await jsonReader.ReadProductsAsync();

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
                productMapper.Update(storeProduct, source, now);
                continue;
            }

            var newStoreProduct = productMapper.Create(
                source,
                category,
                store,
                now
            );
            db.StoreProducts.Add(newStoreProduct);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }


}
