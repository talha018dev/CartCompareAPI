namespace CartCompareAPI.Canonicalization.StoreProducts;

public sealed record StoreProductCanonicalizationSummary(
    int Matched,
    int Created,
    int Unresolved,
    int Failed);