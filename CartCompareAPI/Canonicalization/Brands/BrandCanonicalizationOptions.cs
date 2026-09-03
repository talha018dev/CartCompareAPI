namespace CartCompareAPI.Canonicalization.Brands;

public sealed class BrandCanonicalizationOptions
{
    public const string SectionName = "Canonicalization";

    public List<BrandAliasDefinition> BrandAliases { get; init; } = [];
}
