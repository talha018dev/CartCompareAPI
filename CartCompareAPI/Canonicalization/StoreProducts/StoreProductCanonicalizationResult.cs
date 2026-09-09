namespace CartCompareAPI.Canonicalization.StoreProducts;

public sealed record StoreProductCanonicalizationResult
{
    private StoreProductCanonicalizationResult(
        Guid storeProductId,
        StoreProductCanonicalizationOutcome outcome,
        Guid? productId,
        StoreProductCanonicalizationFailure? failure
    )
    {
        StoreProductId = storeProductId;
        Outcome = outcome;
        ProductId = productId;
        Failure = failure;
    }

    public Guid StoreProductId { get; }
    public StoreProductCanonicalizationOutcome Outcome { get; }
    public Guid? ProductId { get; }
    public StoreProductCanonicalizationFailure? Failure { get; }


    public static StoreProductCanonicalizationResult Matched(
        Guid storeProductId,
        Guid productId
    )
    {
        return new StoreProductCanonicalizationResult(
            storeProductId,
            outcome: StoreProductCanonicalizationOutcome.Matched,
            productId,
            failure: null
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
