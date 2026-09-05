using CartCompareAPI.Canonicalization.Names;

namespace CartCompareAPI.Canonicalization.Packaging;

public sealed class ProductPackageTypeParser(
    IProductNameNormalizer nameNormalizer) : IPackageTypeParser
{
    private static readonly IReadOnlyDictionary<string, string[]> PackageAliases =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["tin"] = ["tin"],
            ["bib"] = ["bib", "bag in box"],
            ["foil"] = ["foil"],
            ["poly"] = ["poly"],
            ["box"] = ["box"],
            ["glass jar"] = ["glass jar"]
        };

    public ParsedPackageType? Parse(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return null;
        }

        string normalizedName = nameNormalizer.Normalize(productName);
        string searchableName = $" {normalizedName} ";

        var aliasMatches = PackageAliases
            .SelectMany(package => package.Value.Select(alias => new
            {
                PackageType = package.Key,
                Alias = alias
            }))
            .Where(candidate =>
                searchableName.Contains(
                    $" {candidate.Alias} ",
                    StringComparison.Ordinal))
            .ToList();

        var matches = aliasMatches
            .Where(candidate => !aliasMatches.Any(other =>
                other.Alias.Length > candidate.Alias.Length &&
                $" {other.Alias} ".Contains(
                    $" {candidate.Alias} ",
                    StringComparison.Ordinal)))
            .Select(candidate => candidate.PackageType)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return matches.Count == 1
            ? new ParsedPackageType(matches[0])
            : null;
    }
}
