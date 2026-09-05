using System.Globalization;

namespace CartCompareAPI.Canonicalization.Products;

public sealed class CanonicalKeyBuilder : ICanonicalKeyBuilder
{
    public string Build(
        string categoryKey,
        NormalizedProduct product,
        bool includePackageDisambiguator = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryKey);
        ArgumentNullException.ThrowIfNull(product);

        string variant = product.Variant is null
            ? string.Empty
            : string.Join(
                "+",
                product.Variant.Values.OrderBy(
                    value => value,
                    StringComparer.Ordinal));

        string quantity = product.Quantity.Value.ToString(
            "0.############################",
            CultureInfo.InvariantCulture);

        string packageDisambiguator = includePackageDisambiguator
            ? product.PackageType?.Value ?? string.Empty
            : string.Empty;

        return string.Join(
            "|",
            categoryKey.Trim().ToLowerInvariant(),
            product.Brand.BrandKey.Trim().ToLowerInvariant(),
            product.NormalizedName,
            variant,
            $"{quantity}-{product.Quantity.Unit}",
            packageDisambiguator);
    }
}
