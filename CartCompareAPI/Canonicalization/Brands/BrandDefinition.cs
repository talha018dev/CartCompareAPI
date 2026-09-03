namespace CartCompareAPI.Canonicalization.Brands;

public sealed record BrandDefinition(
    string Key,
    string DisplayName,
    IReadOnlyCollection<string> Aliases);
