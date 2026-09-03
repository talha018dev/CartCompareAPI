using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Canonicalization.Names;

namespace CartCompareAPI.Tests.Canonicalization.Brands;

public sealed class BrandResolverTests
{
    private readonly BrandResolver resolver = new(
        new ProductNameNormalizer());

    private static readonly BrandDefinition Marks = new(
        Key: "marks",
        DisplayName: "Marks",
        Aliases: ["marks", "mark's"]);

    [Theory]
    [InlineData("MARKS Full Cream Milk Powder 1kg")]
    [InlineData("Marks Full Cream Milk Powder 1kg")]
    [InlineData("mark's full cream milk powder 1kg")]
    public void Resolve_WithKnownBrand_ShouldReturnBrand(string productName)
    {
        BrandResolution? result = resolver.Resolve(
            productName,
            [Marks]);

        Assert.NotNull(result);
        Assert.Equal("marks", result.BrandKey);
        Assert.Equal("Marks", result.DisplayName);
    }

    [Fact]
    public void Resolve_WithAlias_ShouldReportMatchedAlias()
    {
        BrandResolution? result = resolver.Resolve(
            "mark's full cream milk powder",
            [Marks]);

        Assert.NotNull(result);
        Assert.Equal("mark's", result.MatchedAlias);
    }

    [Fact]
    public void Resolve_WithUnknownBrand_ShouldReturnNull()
    {
        BrandResolution? result = resolver.Resolve(
            "Unknown Full Cream Milk Powder",
            [Marks]);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_ShouldNotMatchBrandInsideAnotherWord()
    {
        BrandResolution? result = resolver.Resolve(
            "Landmarks Full Cream Milk Powder",
            [Marks]);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_WithTwoDifferentBrands_ShouldReturnNull()
    {
        var dano = new BrandDefinition(
            Key: "dano",
            DisplayName: "Dano",
            Aliases: ["dano"]);

        BrandResolution? result = resolver.Resolve(
            "Marks and Dano Milk Powder",
            [Marks, dano]);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_WithDisplayName_ShouldMatchWithoutExplicitAlias()
    {
        var brand = new BrandDefinition(
            Key: "marks",
            DisplayName: "Marks",
            Aliases: []);

        BrandResolution? result = resolver.Resolve(
            "MARKS Full Cream Milk Powder",
            [brand]);

        Assert.NotNull(result);
        Assert.Equal("marks", result.BrandKey);
        Assert.Equal("Marks", result.DisplayName);
    }

    [Fact]
    public void Resolve_WithMultiwordBrand_ShouldMatchCompletePhrase()
    {
        var brand = new BrandDefinition(
            Key: "farm-fresh",
            DisplayName: "Farm Fresh",
            Aliases: []);

        BrandResolution? result = resolver.Resolve(
            "Farm Fresh UHT Milk 1L",
            [brand]);

        Assert.NotNull(result);
        Assert.Equal("farm-fresh", result.BrandKey);
    }

    [Fact]
    public void Resolve_WithIncompleteMultiwordBrand_ShouldNotMatch()
    {
        var brand = new BrandDefinition(
            Key: "farm-fresh",
            DisplayName: "Farm Fresh",
            Aliases: []);

        BrandResolution? result = resolver.Resolve(
            "Fresh UHT Milk 1L",
            [brand]);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_WhenAliasMatchesDifferentBrands_ShouldReturnNull()
    {
        var firstBrand = new BrandDefinition(
            Key: "brand-one",
            DisplayName: "Brand One",
            Aliases: ["premium"]);

        var secondBrand = new BrandDefinition(
            Key: "brand-two",
            DisplayName: "Brand Two",
            Aliases: ["premium"]);

        BrandResolution? result = resolver.Resolve(
            "Premium Full Cream Milk",
            [firstBrand, secondBrand]);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_WithBlankProductName_ShouldReturnNull(string productName)
    {
        BrandResolution? result = resolver.Resolve(productName, [Marks]);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_WithNoBrandDefinitions_ShouldReturnNull()
    {
        BrandResolution? result = resolver.Resolve(
            "Marks Full Cream Milk",
            []);

        Assert.Null(result);
    }
}
