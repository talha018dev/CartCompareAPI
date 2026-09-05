using CartCompareAPI.Canonicalization.Variants;

namespace CartCompareAPI.Tests.Canonicalization.Variants;

public sealed class ProductVariantParserTests
{
    private readonly ProductVariantParser parser = new();

    [Theory]
    [InlineData("full cream milk powder", "full cream")]
    [InlineData("strawberry yogurt drink", "strawberry")]
    [InlineData("low fat milk", "low fat")]
    public void Parse_WithKnownVariant_ShouldReturnVariant(
        string normalizedName,
        string expected)
    {
        ParsedVariant? result = parser.Parse(normalizedName);

        Assert.NotNull(result);
        Assert.Contains(expected, result.Values);
    }

    [Fact]
    public void Parse_WithMultipleVariants_ShouldPreserveTheirOrder()
    {
        ParsedVariant? result = parser.Parse(
            "full cream chocolate milk powder");

        Assert.NotNull(result);
        Assert.Equal(["full cream", "chocolate"], result.Values);
    }

    [Fact]
    public void Parse_WithSpecificVariant_ShouldDiscardNestedGenericVariant()
    {
        ParsedVariant? result = parser.Parse(
            "belgian chocolate uht milk");

        Assert.NotNull(result);
        Assert.Equal(["belgian chocolate"], result.Values);
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain milk powder")]
    public void Parse_WithoutKnownVariant_ShouldReturnNull(string normalizedName)
    {
        ParsedVariant? result = parser.Parse(normalizedName);

        Assert.Null(result);
    }
}
