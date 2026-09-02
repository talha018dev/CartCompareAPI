using System;
using CartCompareAPI.Canonicalization.Quantity;

namespace CartCompareAPI.Tests.Canonicalization.Quantity;

public sealed class ProductQuantityParserTests
{

    [Theory]
    [InlineData("Rice 2kg", 2000, "g")]
    [InlineData("Rice 500g", 500, "g")]
    [InlineData("Milk 1l", 1000, "ml")]
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
