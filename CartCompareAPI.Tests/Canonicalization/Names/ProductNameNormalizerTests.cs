using System;
using CartCompareAPI.Canonicalization.Names;

namespace CartCompareAPI.Tests.Canonicalization.Names;

public sealed class ProductNameNormalizerTests
{
    private readonly ProductNameNormalizer normalizer = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void Normalize_WithBlankInput_ShouldReturnEmptyString(string input)
    {
        string result = normalizer.Normalize(input);

        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("Milk-Powder", "milk powder")]
    [InlineData("Milk, Powder", "milk powder")]
    [InlineData("Milk (TIN)", "milk tin")]
    [InlineData("Milk/Powder", "milk powder")]
    [InlineData("Milk: Powder", "milk powder")]
    [InlineData("Milk [UHT]", "milk uht")]
    public void Normalize_ShouldReplacePunctuationWithSpaces(
        string input,
        string expected)
    {
        string result = normalizer.Normalize(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(
    "MARKS Full Cream Milk Powder",
    "marks full cream milk powder")]
    [InlineData(
    "  Marks Full Cream  ",
    "marks full cream")]
    [InlineData(
    "Marks   Full    Cream",
    "marks full cream")]
    [InlineData(
    "Marks\tFull\r\nCream",
    "marks full cream")]
    public void Normalize_ShouldNormalizeCasingAndWhitespace(
    string input,
    string expected)
    {
        string result = normalizer.Normalize(input);

        Assert.Equal(expected, result);
    }



    [Theory]
    [InlineData("Milk 2in1", "milk 2 in 1")]
    [InlineData("Milk 2-in-1", "milk 2 in 1")]
    [InlineData("Milk 2 in 1", "milk 2 in 1")]
    [InlineData("Coffee 3in1", "coffee 3 in 1")]
    [InlineData("Coffee 3IN1", "coffee 3 in 1")]
    [InlineData("Milk 2in1 500ml", "milk 2 in 1 500ml")]
    public void Normalize_ShouldNormalizeInOneExpressions(
    string input,
    string expected)
    {
        string result = normalizer.Normalize(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Normalize_WithNullInput_ShouldReturnEmptyString()
    {
        string result = normalizer.Normalize(null!);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Normalize_WithEquivalentUnicode_ShouldProduceSameResult()
    {
        const string composed = "Café Milk";
        const string decomposed = "Cafe\u0301 Milk";

        string composedResult = normalizer.Normalize(composed);
        string decomposedResult = normalizer.Normalize(decomposed);

        Assert.Equal(composedResult, decomposedResult);
        Assert.Equal("café milk", composedResult);
    }
}
