namespace CartCompareAPI.Canonicalization.Products;

public sealed record ProductNormalizationResult(
    NormalizedProduct? Product,
    ProductNormalizationFailure? Failure)
{
    public bool IsSuccess => Product is not null;

    public static ProductNormalizationResult Success(NormalizedProduct product)
        => new(product, null);

    public static ProductNormalizationResult Unresolved(
        ProductNormalizationFailure failure)
        => new(null, failure);
}
