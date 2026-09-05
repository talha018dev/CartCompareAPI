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
        var store = catalog.Store;
        var now = DateTime.UtcNow;

        var existingStoreProducts = await db.StoreProducts
            .Where(x => x.StoreId == store.Id)
            .ToDictionaryAsync(x => x.ExternalProductId, cancellationToken);

        foreach (var source in sourceProducts)
        {
            if (existingStoreProducts.TryGetValue(source.Sku, out var storeProduct))
            {
                productMapper.Update(storeProduct, source, now);
                continue;
            }

            var newStoreProduct = productMapper.Create(
                source,
                store,
                now
            );
            db.StoreProducts.Add(newStoreProduct);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }


}
