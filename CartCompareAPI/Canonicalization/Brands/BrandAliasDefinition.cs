namespace CartCompareAPI.Canonicalization.Brands;

public sealed record BrandAliasDefinition(
    string BrandKey,
    IReadOnlyCollection<string> Aliases);
