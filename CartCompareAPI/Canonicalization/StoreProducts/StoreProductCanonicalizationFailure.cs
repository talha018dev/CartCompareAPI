namespace CartCompareAPI.Canonicalization.StoreProducts;

public enum StoreProductCanonicalizationFailure
{
    MissingName,
    BrandNotResolved,
    QuantityNotResolved,
    NormalizedNameEmpty,
    BrandRecordNotFound,
    ConflictingCanonicalProducts,
    PersistenceFailed
}
