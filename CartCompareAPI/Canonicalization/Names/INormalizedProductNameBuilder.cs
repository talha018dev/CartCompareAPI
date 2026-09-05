using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Canonicalization.Packaging;
using CartCompareAPI.Canonicalization.Quantity;

namespace CartCompareAPI.Canonicalization.Names;

public interface INormalizedProductNameBuilder
{
    string Build(
        string productName,
        BrandResolution brand,
        ParsedQuantity quantity,
        ParsedPackageType? packageType);
}
