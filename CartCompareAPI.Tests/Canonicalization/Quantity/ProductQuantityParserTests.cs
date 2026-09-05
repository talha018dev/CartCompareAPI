using System;
using CartCompareAPI.Canonicalization.Quantity;

namespace CartCompareAPI.Tests.Canonicalization.Quantity;

public sealed class ProductQuantityParserTests
{
    public ProductQuantityParser parser = new ProductQuantityParser();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Milk Powder")]
    [InlineData("Milk Powder large pack")]
    [InlineData("Milk Powder 0kg")]
    public void Parse_WithNoReliableQuantity_ReturnsNull(string productName)
    {
        ParsedQuantity? result = parser.Parse(productName);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_WithMultipleQuantities_ReturnsNull()
    {
        const string productName = "Buy 500ml and get 100ml free";

        ParsedQuantity? result = parser.Parse(productName);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_WithNullName_ReturnsNull()
    {
        ParsedQuantity? result = parser.Parse(null!);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("Rice 2kg", 2000, "g")]
    [InlineData("Rice 500g", 500, "g")]
    [InlineData("Milk 1l", 1000, "ml")]
    [InlineData("Milk 1.5L", 1500, "ml")]
    [InlineData("Milk 500 ml", 500, "ml")]
    public void Parse_ShouldNormalizeQuantity(
            string input,
            decimal expectedValue,
            string expectedUnit
        )
    {
        var parser = new ProductQuantityParser();
        var result = parser.Parse(input);

        Assert.NotNull(result);
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(expectedUnit, result.Unit);
    }

    [Theory]
    [InlineData("Milk 1Ltr.", 1000, "ml")]
    [InlineData("Milk 1 litre", 1000, "ml")]
    [InlineData("Cheese 24 Portions", 24, "count")]
    public void Parse_WithAdditionalCatalogUnits_ShouldNormalizeQuantity(
        string input,
        decimal expectedValue,
        string expectedUnit)
    {
        ParsedQuantity? result = parser.Parse(input);

        Assert.NotNull(result);
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(expectedUnit, result.Unit);
    }

    [Theory]
    [InlineData("Ghee 400(±)50gm")]
    [InlineData("Condensed Milk 397(±)3gm")]
    public void Parse_WithToleranceQuantity_ShouldReturnNull(string input)
    {
        ParsedQuantity? result = parser.Parse(input);

        Assert.Null(result);
    }

}
