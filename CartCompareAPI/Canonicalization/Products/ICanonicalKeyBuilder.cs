namespace CartCompareAPI.Canonicalization.Products;

public interface ICanonicalKeyBuilder
{
    string Build(
        string categoryKey,
        NormalizedProduct product,
        bool includePackageDisambiguator = false);
}
