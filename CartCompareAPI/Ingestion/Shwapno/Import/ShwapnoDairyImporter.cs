using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;
using CartCompareAPI.Ingestion.Shwapno.Entities;
using CartCompareAPI.Ingestion.Shwapno.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CartCompareAPI.Ingestion.Shwapno;

public sealed class ShwapnoDairyImporter(
        AppDbContext db,
        ShwapnoCatalogInitializer catalogInitializer,
        ShwapnoProductMapper productMapper
    ) : IShwapnoProductImporter
{

    public async Task<ShwapnoImportResult> ImportAsync(
        string categorySlug,
        IReadOnlyCollection<ShwapnoProduct> sourceProducts,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        int createdProductCount = 0;
        int updatedProductCount = 0;

        if (string.IsNullOrWhiteSpace(categorySlug) ||
            categorySlug.Any(character => !char.IsLetterOrDigit(character) && character != '-'))
        {
            throw new UnsupportedShwapnoCategoryException(
                "Category must contain only letters, numbers, and hyphens.");
        }

        if (sourceProducts is null || sourceProducts.Count == 0)
        {
            throw new InvalidShwapnoSourceDataException(
                "Shwapno returned no products.");
        }


        foreach (ShwapnoProduct? source in sourceProducts)
        {
            if (source is null || string.IsNullOrWhiteSpace(source.Sku))
            {
                throw new InvalidShwapnoSourceDataException(
                    "A Shwapno product is missing its SKU.");
            }

            if (source.Price is null || source.Price.PriceValue <= 0)
            {
                throw new InvalidShwapnoSourceDataException(
                    "A Shwapno product is missing a valid price.");
            }
        }

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        ShwapnoCatalog catalog = await catalogInitializer.ShwapnoCatalogInitializedAsync(categorySlug, cancellationToken);
        Store store = catalog.Store;
        DateTime now = DateTime.UtcNow;

        Dictionary<string, StoreProduct> existingStoreProducts = await db.StoreProducts
            .Where(x => x.StoreId == store.Id)
            .ToDictionaryAsync(x => x.ExternalProductId, cancellationToken);

        foreach (ShwapnoProduct source in sourceProducts)
        {
            if (existingStoreProducts.TryGetValue(source.Sku, out StoreProduct? storeProduct))
            {
                productMapper.Update(storeProduct, source, now);
                updatedProductCount++;
                continue;
            }

            StoreProduct newStoreProduct = productMapper.Create(
                source,
                store,
                catalog.Category,
                now
            );
            db.StoreProducts.Add(newStoreProduct);
            createdProductCount++;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        ShwapnoImportSummary summary = new(
            Received: sourceProducts.Count,
            Created: createdProductCount,
            Updated: updatedProductCount
        );

        return new ShwapnoImportResult(summary, store.Id, catalog.Category.Id);
    }
}
