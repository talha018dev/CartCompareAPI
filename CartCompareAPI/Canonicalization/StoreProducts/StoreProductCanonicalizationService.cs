using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Canonicalization.StoreProducts;

public class StoreProductCanonicalizationService(AppDbContext db,
    IStoreProductCanonicalizer canonicalizer,
    IBrandDefinitionProvider brandDefinitionProvider,
    TimeProvider timeProvider)
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
            .Include(storeProduct => storeProduct.Store)
            .Where(p => p.StoreId == storeId && p.SourceCategoryId == categoryId && p.ProductId == null)
            .OrderBy(p => p.ExternalProductId)
            .ToListAsync(cancellationToken);

        Guid[] storeProductIds = storeProducts
            .Select(storeProduct => storeProduct.Id)
            .ToArray();
        Dictionary<Guid, StoreProductCanonicalizationIssue> existingIssues =
            await db.StoreProductCanonicalizationIssues
                .Where(issue => storeProductIds.Contains(issue.StoreProductId))
                .ToDictionaryAsync(
                    issue => issue.StoreProductId,
                    cancellationToken);

        BrandDefinition[] brandDefinitions = (await brandDefinitionProvider
            .GetAllAsync(cancellationToken))
            .ToArray();

        int matched = 0;
        int created = 0;
        int unresolved = 0;
        int failed = 0;

        foreach (StoreProduct storeProduct in storeProducts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            StoreProductCanonicalizationResult result = await canonicalizer.CanonicalizeAsync(storeProduct, category, brandDefinitions, cancellationToken);

            switch (result.Outcome)
            {
                case StoreProductCanonicalizationOutcome.Matched:
                    matched++;
                    RemoveIssue(storeProduct.Id, existingIssues);
                    break;

                case StoreProductCanonicalizationOutcome.Created:
                    created++;
                    RemoveIssue(storeProduct.Id, existingIssues);
                    // The next listing must be able to find this product in the database.
                    await db.SaveChangesAsync(cancellationToken);
                    break;

                case StoreProductCanonicalizationOutcome.Unresolved:
                    unresolved++;
                    RecordIssue(storeProduct, result, existingIssues);
                    break;

                case StoreProductCanonicalizationOutcome.Failed:
                    failed++;
                    RecordIssue(storeProduct, result, existingIssues);
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
            Failed: failed);
    }

    private void RecordIssue(
        StoreProduct storeProduct,
        StoreProductCanonicalizationResult result,
        IDictionary<Guid, StoreProductCanonicalizationIssue> existingIssues)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        if (existingIssues.TryGetValue(storeProduct.Id, out var issue))
        {
            issue.StoreProductName = storeProduct.StoreProductName;
            issue.StoreName = storeProduct.Store.Name;
            issue.Outcome = result.Outcome;
            issue.FailureReason = result.Failure!.Value;
            issue.AttemptCount++;
            issue.LastOccurredAt = now;
            return;
        }

        issue = new StoreProductCanonicalizationIssue
        {
            Id = Guid.NewGuid(),
            StoreProductId = storeProduct.Id,
            StoreProductName = storeProduct.StoreProductName,
            StoreId = storeProduct.StoreId,
            StoreName = storeProduct.Store.Name,
            Outcome = result.Outcome,
            FailureReason = result.Failure!.Value,
            AttemptCount = 1,
            FirstOccurredAt = now,
            LastOccurredAt = now
        };
        db.StoreProductCanonicalizationIssues.Add(issue);
        existingIssues.Add(storeProduct.Id, issue);
    }

    private void RemoveIssue(
        Guid storeProductId,
        IDictionary<Guid, StoreProductCanonicalizationIssue> existingIssues)
    {
        if (!existingIssues.Remove(storeProductId, out var issue))
        {
            return;
        }

        db.StoreProductCanonicalizationIssues.Remove(issue);
    }
}
