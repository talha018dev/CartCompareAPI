using System;

namespace CartCompareAPI.Canonicalization.StoreProducts;

public interface IStoreProductCanonicalizationService
{
    Task<StoreProductCanonicalizationSummary> CanonicalizePendingAsync(
        CancellationToken cancellationToken = default);
}
