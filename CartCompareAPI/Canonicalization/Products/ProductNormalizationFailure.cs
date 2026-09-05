namespace CartCompareAPI.Canonicalization.Products;

public enum ProductNormalizationFailure
{
    MissingName,
    BrandNotResolved,
    QuantityNotResolved,
    NormalizedNameEmpty
}
