namespace CartCompareAPI.Canonicalization.Variants;

public sealed class ProductVariantParser : IVariantParser
{
    private static readonly string[] KnownVariants =
    [
        "belgian chocolate",
        "cookies and cream",
        "full cream",
        "non fat",
        "low fat",
        "chocolate",
        "strawberry",
        "aloe vera",
        "unsalted",
        "skimmed",
        "vanilla",
        "diabetic",
        "classic",
        "original",
        "garlic",
        "mango",
        "mocha",
        "orange",
        "pizza",
        "sweet",
        "sour"
    ];

    public ParsedVariant? Parse(string normalizedProductName)
    {
        if (string.IsNullOrWhiteSpace(normalizedProductName))
        {
            return null;
        }

        string searchableName = $" {normalizedProductName} ";

        var matches = KnownVariants
            .Where(variant => searchableName.Contains(
                $" {variant} ",
                StringComparison.Ordinal))
            .Where(variant => !KnownVariants.Any(other =>
                other.Length > variant.Length
                && searchableName.Contains(
                    $" {other} ",
                    StringComparison.Ordinal)
                && $" {other} ".Contains(
                    $" {variant} ",
                    StringComparison.Ordinal)))
            .OrderBy(variant => normalizedProductName.IndexOf(
                variant,
                StringComparison.Ordinal))
            .ToList();

        return matches.Count == 0
            ? null
            : new ParsedVariant(matches);
    }
}
