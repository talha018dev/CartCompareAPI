namespace CartCompareAPI.Canonicalization.StoreProducts;

public sealed record StoreProductCanonicalizationResult(
    Guid StoreProductId,
    StoreProductCanonicalizationOutcome Outcome,
    Guid? ProductId,
    StoreProductCanonicalizationFailure? Failure)
{
    public static StoreProductCanonicalizationResult Matched(
        Guid storeProductId,
        Guid productId
    )
    {
        return new StoreProductCanonicalizationResult(
            StoreProductId: storeProductId,
            Outcome: StoreProductCanonicalizationOutcome.Matched,
            ProductId: productId,
            Failure: null
        );
    }

    public static StoreProductCanonicalizationResult Created(
        Guid storeProductId,
        Guid productId)
    {
        return new StoreProductCanonicalizationResult(
            storeProductId,
            StoreProductCanonicalizationOutcome.Created,
            productId,
            null);
    }

    public static StoreProductCanonicalizationResult Unresolved(
        Guid storeProductId,
        StoreProductCanonicalizationFailure failure)
    {
        return new StoreProductCanonicalizationResult(
            storeProductId,
            StoreProductCanonicalizationOutcome.Unresolved,
            null,
            failure);
    }
}
