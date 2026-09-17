using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Canonicalization.StoreProducts;

public class StoreProductCanonicalizationService(AppDbContext db,
    IStoreProductCanonicalizer canonicalizer,
    IBrandDefinitionProvider brandDefinitionProvider)
    : IStoreProductCanonicalizationService
{
    public async Task<StoreProductCanonicalizationSummary> CanonicalizePendingAsync(
        Guid storeId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        Category category = await db.Categories.SingleAsync(
            category => category.Id == categoryId, cancellationToken);

        List<StoreProduct> storeProducts = await db.StoreProducts
            .Where(p => p.StoreId == storeId && p.SourceCategoryId == categoryId && p.ProductId == null)
            .OrderBy(p => p.ExternalProductId)
            .ToListAsync(cancellationToken);

        BrandDefinition[] brandDefinitions = (await brandDefinitionProvider
            .GetAllAsync(cancellationToken))
            .ToArray();

        int matched = 0;
        int created = 0;
        int unresolved = 0;

        foreach (StoreProduct storeProduct in storeProducts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            StoreProductCanonicalizationResult result = await canonicalizer.CanonicalizeAsync(storeProduct, category, brandDefinitions, cancellationToken);

            switch (result.Outcome)
            {
                case StoreProductCanonicalizationOutcome.Matched:
                    matched++;
                    break;

                case StoreProductCanonicalizationOutcome.Created:
                    created++;
                    // The next listing must be able to find this product in the database.
                    await db.SaveChangesAsync(cancellationToken);
                    break;

                case StoreProductCanonicalizationOutcome.Unresolved:
                    unresolved++;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return new StoreProductCanonicalizationSummary(
            Matched: matched,
            Created: created,
            Unresolved: unresolved,
            Failed: 0);
    }
}
