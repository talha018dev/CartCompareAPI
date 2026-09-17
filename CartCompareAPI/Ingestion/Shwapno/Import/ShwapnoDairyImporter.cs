using CartCompareAPI.Infrastructure.Data;
using CartCompareAPI.Ingestion.Shwapno.Entities;
using CartCompareAPI.Ingestion.Shwapno.Import;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Ingestion.Shwapno;

public sealed class ShwapnoDairyImporter(
        AppDbContext db,
        ShwapnoCatalogInitializer catalogInitializer,
        ShwapnoProductMapper productMapper
    )
{

    public async Task ImportAsync(string categorySlug,
    IReadOnlyCollection<ShwapnoProduct> sourceProducts,
    CancellationToken cancellationToken = default)
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
            throw new ArgumentException("At least one product is required.", nameof(sourceProducts));

        foreach (var source in sourceProducts)
        {
            if (source is null || string.IsNullOrWhiteSpace(source.Sku))
                throw new ArgumentException("Every product must have a SKU.", nameof(sourceProducts));

            if (source.Price is null || source.Price.PriceValue <= 0)
                throw new ArgumentException("Every product must have a positive price.", nameof(sourceProducts));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var catalog = await catalogInitializer.ShwapnoCatalogInitializedAsync(categorySlug, cancellationToken);
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
