using System;
using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Canonicalization.StoreProducts;

public class StoreProductCanonicalizationService(AppDbContext db,
    IStoreProductCanonicalizer canonicalizer,
    IBrandDefinitionProvider brandDefinitionProvider)
    : IStoreProductCanonicalizationService
{
    public async Task<StoreProductCanonicalizationSummary> CanonicalizePendingAsync(CancellationToken cancellationToken = default)
    {

        // 1. Load unresolved StoreProducts.
        // 2. Load brand definitions.
        // 3. Call canonicalizer for each listing.
        // 4. Save the batch here.

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            // when (IsCanonicalKeyViolation(exception))
        {
            // Recover from the race.
        }

        // 5. Return summary.
        throw new NotImplementedException();
    }
}
