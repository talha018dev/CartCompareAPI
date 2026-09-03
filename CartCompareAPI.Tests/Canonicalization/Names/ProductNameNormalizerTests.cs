using System;
using CartCompareAPI.Canonicalization.Names;

namespace CartCompareAPI.Tests.Canonicalization.Names;

public sealed class ProductNameNormalizerTests
{
    private readonly ProductNameNormalizer normalizer = new();


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
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void Normalize_WithBlankInput_ShouldReturnEmptyString(string input)
    {
        string result = normalizer.Normalize(input);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Normalize_WithNullInput_ShouldReturnEmptyString()
    {
        string result = normalizer.Normalize(null!);

        Assert.Equal(string.Empty, result);
    }
}
