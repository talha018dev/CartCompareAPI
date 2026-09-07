using CartCompareAPI.Canonicalization.Products;

namespace CartCompareAPI.Canonicalization.StoreProducts;

public static class StoreProductCanonicalizationFailureExtensions
{
    public static StoreProductCanonicalizationFailure ToStoreProductFailure(
        this ProductNormalizationFailure failure)
    {
        return failure switch
        {
            ProductNormalizationFailure.MissingName =>
                StoreProductCanonicalizationFailure.MissingName,
            ProductNormalizationFailure.BrandNotResolved =>
                StoreProductCanonicalizationFailure.BrandNotResolved,
            ProductNormalizationFailure.QuantityNotResolved =>
                StoreProductCanonicalizationFailure.QuantityNotResolved,
            ProductNormalizationFailure.NormalizedNameEmpty =>
                StoreProductCanonicalizationFailure.NormalizedNameEmpty,
            _ => throw new ArgumentOutOfRangeException(
                nameof(failure),
                failure,
                "Unknown product normalization failure.")
        };
    }
}
