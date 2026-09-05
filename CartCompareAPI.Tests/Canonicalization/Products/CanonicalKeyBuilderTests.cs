using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Canonicalization.Packaging;
using CartCompareAPI.Canonicalization.Products;
using CartCompareAPI.Canonicalization.Quantity;
using CartCompareAPI.Canonicalization.Variants;

namespace CartCompareAPI.Tests.Canonicalization.Products;

public sealed class CanonicalKeyBuilderTests
{
    private readonly CanonicalKeyBuilder builder = new();

    [Fact]
    public void Build_ShouldUseInspectableSegmentStructure()
    {
        NormalizedProduct product = CreateProduct(
            quantity: 1000,
            unit: "g",
            packageType: "tin",
            variants: ["full cream"]);

        string key = builder.Build("Dairy", product);

        Assert.Equal(
            "dairy|marks|full cream milk powder|full cream|1000-g|",
            key);
    }

    [Fact]
    public void Build_ShouldIncludePackageOnlyWhenRequested()
    {
        NormalizedProduct product = CreateProduct(
            quantity: 1000,
            unit: "g",
            packageType: "tin",
            variants: ["full cream"]);

        string withoutPackage = builder.Build("dairy", product);
        string withPackage = builder.Build(
            "dairy",
            product,
            includePackageDisambiguator: true);

        Assert.EndsWith("|", withoutPackage);
        Assert.EndsWith("|tin", withPackage);
    }

    [Fact]
    public void Build_ShouldOrderVariantsDeterministically()
    {
        NormalizedProduct first = CreateProduct(
            500,
            "g",
            null,
            ["full cream", "chocolate"]);
        NormalizedProduct second = CreateProduct(
            500,
            "g",
            null,
            ["chocolate", "full cream"]);

        Assert.Equal(
            builder.Build("dairy", first),
            builder.Build("dairy", second));
    }

    [Fact]
    public void Build_WithDifferentQuantity_ShouldProduceDifferentKey()
    {
        NormalizedProduct small = CreateProduct(
            400,
            "g",
            null,
            ["full cream"]);
        NormalizedProduct large = CreateProduct(
            1000,
            "g",
            null,
            ["full cream"]);

        Assert.NotEqual(
            builder.Build("dairy", small),
            builder.Build("dairy", large));
    }

    private static NormalizedProduct CreateProduct(
        decimal quantity,
        string unit,
        string? packageType,
        IReadOnlyList<string> variants)
    {
        return new NormalizedProduct(
            "MARKS Full Cream Milk Powder",
            "full cream milk powder",
            new BrandResolution("marks", "Marks", "Marks"),
            new ParsedQuantity(quantity, unit, $"{quantity}{unit}"),
            packageType is null
                ? null
                : new ParsedPackageType(packageType, packageType),
            variants.Count == 0 ? null : new ParsedVariant(variants));
    }
}
