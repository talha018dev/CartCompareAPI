using CartCompareAPI.Canonicalization.StoreProducts;

namespace CartCompareAPI.Tests.Canonicalization.StoreProducts;

public sealed class StoreProductCanonicalizationResultTests
{
    [Fact]
    public void Matched_ShouldContainBothIdsAndNoFailure()
    {
        var storeProductId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        StoreProductCanonicalizationResult result =
            StoreProductCanonicalizationResult.Matched(
                storeProductId,
                productId);

        Assert.Equal(storeProductId, result.StoreProductId);
        Assert.Equal(StoreProductCanonicalizationOutcome.Matched, result.Outcome);
        Assert.Equal(productId, result.ProductId);
        Assert.Null(result.Failure);
    }

    [Fact]
    public void Created_ShouldContainBothIdsAndNoFailure()
    {
        var storeProductId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        StoreProductCanonicalizationResult result =
            StoreProductCanonicalizationResult.Created(
                storeProductId,
                productId);

        Assert.Equal(storeProductId, result.StoreProductId);
        Assert.Equal(StoreProductCanonicalizationOutcome.Created, result.Outcome);
        Assert.Equal(productId, result.ProductId);
        Assert.Null(result.Failure);
    }

    [Fact]
    public void Unresolved_ShouldContainStoreProductIdAndFailureWithoutProductId()
    {
        var storeProductId = Guid.NewGuid();

        StoreProductCanonicalizationResult result =
            StoreProductCanonicalizationResult.Unresolved(
                storeProductId,
                StoreProductCanonicalizationFailure.BrandNotResolved);

        Assert.Equal(storeProductId, result.StoreProductId);
        Assert.Equal(StoreProductCanonicalizationOutcome.Unresolved, result.Outcome);
        Assert.Null(result.ProductId);
        Assert.Equal(
            StoreProductCanonicalizationFailure.BrandNotResolved,
            result.Failure);
    }
}
