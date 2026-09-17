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
    )
{

    public async Task ImportAsync(
        string categorySlug,
        IReadOnlyCollection<ShwapnoProduct> sourceProducts,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(categorySlug) ||
            categorySlug.Any(character => !char.IsLetterOrDigit(character) && character != '-'))
        {
            throw new ArgumentException(
                "Category must contain only letters, numbers, and hyphens.",
                nameof(categorySlug));
        }

        ArgumentNullException.ThrowIfNull(sourceProducts);
        if (sourceProducts.Count == 0)
        {
            throw new ArgumentException("At least one product is required.", nameof(sourceProducts));
        }

        foreach (ShwapnoProduct? source in sourceProducts)
        {
            if (source is null || string.IsNullOrWhiteSpace(source.Sku))
            {
                throw new ArgumentException("Every product must have a SKU.", nameof(sourceProducts));
            }

            if (source.Price is null || source.Price.PriceValue <= 0)
            {
                throw new ArgumentException("Every product must have a positive price.", nameof(sourceProducts));
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
                continue;
            }

            StoreProduct newStoreProduct = productMapper.Create(
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
