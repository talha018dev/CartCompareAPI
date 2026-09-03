using System;
using CartCompareApi.Canonicalization.Quantity;
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

}
