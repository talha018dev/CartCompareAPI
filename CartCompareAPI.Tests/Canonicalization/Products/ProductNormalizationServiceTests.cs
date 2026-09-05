using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Canonicalization.Names;
using CartCompareAPI.Canonicalization.Packaging;
using CartCompareAPI.Canonicalization.Products;
using CartCompareAPI.Canonicalization.Quantity;
using CartCompareAPI.Canonicalization.Variants;

namespace CartCompareAPI.Tests.Canonicalization.Products;

public sealed class ProductNormalizationServiceTests
{
    private readonly ProductNormalizationService service;

    private static readonly BrandDefinition Marks = new(
        "marks",
        "Marks",
        ["mark's"]);

    public ProductNormalizationServiceTests()
    {
        var nameNormalizer = new ProductNameNormalizer();
        service = new ProductNormalizationService(
            new BrandResolver(nameNormalizer),
            new ProductQuantityParser(),
            new ProductPackageTypeParser(nameNormalizer),
            new NormalizedProductNameBuilder(nameNormalizer),
            new ProductVariantParser());
    }

    [Fact]
    public void Normalize_WithCompleteTitle_ShouldReturnNormalizedProduct()
    {
        ProductNormalizationResult result = service.Normalize(
            "MARKS Full Cream Milk Powder 1kg (TIN)",
            [Marks]);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Product);
        Assert.Equal("full cream milk powder", result.Product.NormalizedName);
        Assert.Equal("marks", result.Product.Brand.BrandKey);
        Assert.Equal(1000, result.Product.Quantity.Value);
        Assert.Equal("g", result.Product.Quantity.Unit);
        Assert.Equal("tin", result.Product.PackageType?.Value);
        Assert.Equal(["full cream"], result.Product.Variant?.Values);
    }

    [Fact]
    public void Normalize_WithUnknownBrand_ShouldBeUnresolved()
    {
        ProductNormalizationResult result = service.Normalize(
            "Unknown Full Cream Milk Powder 1kg",
            [Marks]);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ProductNormalizationFailure.BrandNotResolved,
            result.Failure);
    }

    [Fact]
    public void Normalize_WithAmbiguousQuantity_ShouldBeUnresolved()
    {
        ProductNormalizationResult result = service.Normalize(
            "Marks Ghee 400(±)50gm",
            [Marks]);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ProductNormalizationFailure.QuantityNotResolved,
            result.Failure);
    }
}
