using System.Text.RegularExpressions;
using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Canonicalization.Packaging;
using CartCompareAPI.Canonicalization.Quantity;

namespace CartCompareAPI.Canonicalization.Names;

public sealed class NormalizedProductNameBuilder(
    IProductNameNormalizer nameNormalizer)
    : INormalizedProductNameBuilder
{
    private static readonly Regex BuyGetPromotion = new(
        @"\bbuy\s*\d+\s+get\s*\d+\s+free\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string Build(
        string productName,
        BrandResolution brand,
        ParsedQuantity quantity,
        ParsedPackageType? packageType)
    {
        string normalizedName = nameNormalizer.Normalize(productName);

        normalizedName = RemovePhrase(
            normalizedName,
            nameNormalizer.Normalize(brand.MatchedAlias));
        normalizedName = RemovePhrase(
            normalizedName,
            nameNormalizer.Normalize(quantity.MatchedText));

        if (packageType is not null)
        {
            normalizedName = RemovePhrase(
                normalizedName,
                nameNormalizer.Normalize(packageType.MatchedAlias));
            normalizedName = RemovePhrase(normalizedName, "pack");
        }

        normalizedName = BuyGetPromotion.Replace(normalizedName, " ");

        return nameNormalizer.Normalize(normalizedName);
    }

    private static string RemovePhrase(string source, string phrase)
    {
        if (phrase.Length == 0)
        {
            return source;
        }

        return $" {source} ".Replace(
            $" {phrase} ",
            " ",
            StringComparison.Ordinal);
    }
}
