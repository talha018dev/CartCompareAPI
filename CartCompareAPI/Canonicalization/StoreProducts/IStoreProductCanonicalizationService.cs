using System;

namespace CartCompareAPI.Canonicalization.StoreProducts;

public interface IStoreProductCanonicalizationService
{
    Task<StoreProductCanonicalizationSummary> CanonicalizePendingAsync(
        Guid storeId,
        Guid categoryId,
        CancellationToken cancellationToken = default);
}
