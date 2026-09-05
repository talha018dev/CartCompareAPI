using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Canonicalization.Names;
using CartCompareAPI.Canonicalization.Packaging;
using CartCompareAPI.Canonicalization.Quantity;

namespace CartCompareAPI.Tests.Canonicalization.Names;

public sealed class NormalizedProductNameBuilderTests
{
    private readonly NormalizedProductNameBuilder builder = new(
        new ProductNameNormalizer());

    [Fact]
    public void Build_ShouldRemoveBrandQuantityAndPackage()
    {
        string result = builder.Build(
            "MARKS Full Cream Milk Powder 1kg (TIN)",
            new BrandResolution("marks", "Marks", "Marks"),
            new ParsedQuantity(1000, "g", "1kg"),
            new ParsedPackageType("tin", "tin"));

        Assert.Equal("full cream milk powder", result);
    }

    [Fact]
    public void Build_ShouldRetainMeaningfulDescriptors()
    {
        string result = builder.Build(
            "Marks Belgian Chocolate UHT Milk 200ml",
            new BrandResolution("marks", "Marks", "Marks"),
            new ParsedQuantity(200, "ml", "200ml"),
            null);

        Assert.Equal("belgian chocolate uht milk", result);
    }

    [Fact]
    public void Build_ShouldRemovePackageSuffixAndPromotion()
    {
        string result = builder.Build(
            "RD Mango Drinks 200ml (Buy3 Get1 Free) Poly Pack",
            new BrandResolution("rd", "RD", "RD"),
            new ParsedQuantity(200, "ml", "200ml"),
            new ParsedPackageType("poly", "poly"));

        Assert.Equal("mango drinks", result);
    }
}
