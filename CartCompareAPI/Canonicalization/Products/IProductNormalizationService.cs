using CartCompareAPI.Canonicalization.Brands;

namespace CartCompareAPI.Canonicalization.Products;

public interface IProductNormalizationService
{
    ProductNormalizationResult Normalize(
        string productName,
        IReadOnlyCollection<BrandDefinition> brands);
}
