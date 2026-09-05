using CartCompareAPI.Canonicalization.Names;
using CartCompareAPI.Canonicalization.Packaging;

namespace CartCompareAPI.Tests.Canonicalization.Packaging;

public sealed class ProductPackageTypeParserTests
{
    private readonly ProductPackageTypeParser parser = new(
        new ProductNameNormalizer());

    [Theory]
    [InlineData("MARKS Full Cream Milk Powder 1kg (TIN)", "tin")]
    [InlineData("Dano Power Milk Powder 1kg (BIB)", "bib")]
    [InlineData("Milk Powder 1kg Bag-in-Box", "bib")]
    [InlineData("Diploma Milk Powder 500gm (Foil Pack)", "foil")]
    [InlineData("Danish Milk Powder 500gm (Poly)", "poly")]
    [InlineData("Butter 50gmX4Pcs (Box)", "box")]
    [InlineData("Ghee 150gm (Glass Jar)", "glass jar")]
    public void Parse_WithKnownPackage_ShouldReturnNormalizedType(
        string productName,
        string expected)
    {
        ParsedPackageType? result = parser.Parse(productName);

        Assert.NotNull(result);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Fresh Milk 1L")]
    [InlineData("Tinted Milk Bottle 1L")]
    public void Parse_WithoutKnownPackage_ShouldReturnNull(string productName)
    {
        ParsedPackageType? result = parser.Parse(productName);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_WithDifferentPackageTypes_ShouldReturnNull()
    {
        ParsedPackageType? result = parser.Parse(
            "Milk Powder 1kg Tin Foil Pack");

        Assert.Null(result);
    }

    [Fact]
    public void Parse_WithNullName_ShouldReturnNull()
    {
        ParsedPackageType? result = parser.Parse(null!);

        Assert.Null(result);
    }
}
