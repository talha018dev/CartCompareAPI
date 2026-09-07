using CartCompareAPI.Canonicalization.Products;
using CartCompareAPI.Canonicalization.StoreProducts;

namespace CartCompareAPI.Tests.Canonicalization.StoreProducts;

public sealed class StoreProductCanonicalizationFailureExtensionsTests
{
    [Theory]
    [InlineData(
        ProductNormalizationFailure.MissingName,
        StoreProductCanonicalizationFailure.MissingName)]
    [InlineData(
        ProductNormalizationFailure.BrandNotResolved,
        StoreProductCanonicalizationFailure.BrandNotResolved)]
    [InlineData(
        ProductNormalizationFailure.QuantityNotResolved,
        StoreProductCanonicalizationFailure.QuantityNotResolved)]
    [InlineData(
        ProductNormalizationFailure.NormalizedNameEmpty,
        StoreProductCanonicalizationFailure.NormalizedNameEmpty)]
    public void ToStoreProductFailure_ShouldMapNormalizationFailure(
        ProductNormalizationFailure source,
        StoreProductCanonicalizationFailure expected)
    {
        StoreProductCanonicalizationFailure result =
            source.ToStoreProductFailure();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToStoreProductFailure_WithUnknownValue_ShouldThrow()
    {
        var unknown = (ProductNormalizationFailure)999;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => unknown.ToStoreProductFailure());

        Assert.Equal("failure", exception.ParamName);
        Assert.Equal(unknown, exception.ActualValue);
    }
}
