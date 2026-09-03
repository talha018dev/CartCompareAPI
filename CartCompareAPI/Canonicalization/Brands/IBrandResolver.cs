namespace CartCompareAPI.Canonicalization.Brands;

public interface IBrandResolver
{
    BrandResolution? Resolve(
        string productName,
        IReadOnlyCollection<BrandDefinition> brands);
}
