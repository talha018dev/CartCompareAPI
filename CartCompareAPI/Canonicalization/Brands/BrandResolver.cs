using CartCompareAPI.Canonicalization.Names;

namespace CartCompareAPI.Canonicalization.Brands;

public sealed class BrandResolver(IProductNameNormalizer nameNormalizer)
    : IBrandResolver
{
    public BrandResolution? Resolve(
        string productName,
        IReadOnlyCollection<BrandDefinition> brands)
    {
        if (string.IsNullOrWhiteSpace(productName) || brands.Count == 0)
        {
            return null;
        }

        string normalizedProductName = nameNormalizer.Normalize(productName);
        string searchableName = $" {normalizedProductName} ";

        var matches = brands
            .SelectMany(brand =>
                brand.Aliases
                .Append(brand.DisplayName)
                .Select(alias => new
                {
                    Brand = brand,
                    Alias = alias,
                    NormalizedAlias = nameNormalizer.Normalize(alias)
                }))
            .Where(candidate =>
                candidate.NormalizedAlias.Length > 0 &&
                searchableName.Contains(
                    $" {candidate.NormalizedAlias} ",
                    StringComparison.Ordinal))
            .ToList();

        var matchedBrands = matches
            .Select(match => match.Brand.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (matchedBrands.Count != 1)
        {
            return null;
        }

        var selectedMatch = matches
            .Where(match => string.Equals(
                match.Brand.Key,
                matchedBrands[0],
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(match => match.NormalizedAlias.Length)
            .First();

        return new BrandResolution(
            selectedMatch.Brand.Key,
            selectedMatch.Brand.DisplayName,
            selectedMatch.Alias);
    }
}
