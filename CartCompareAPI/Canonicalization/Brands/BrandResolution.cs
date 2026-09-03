namespace CartCompareAPI.Canonicalization.Brands;

public sealed record BrandResolution(
    string BrandKey,
    string DisplayName,
    string MatchedAlias);
