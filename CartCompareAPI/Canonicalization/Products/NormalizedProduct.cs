using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Canonicalization.Packaging;
using CartCompareAPI.Canonicalization.Quantity;
using CartCompareAPI.Canonicalization.Variants;

namespace CartCompareAPI.Canonicalization.Products;

public sealed record NormalizedProduct(
    string SourceName,
    string NormalizedName,
    BrandResolution Brand,
    ParsedQuantity Quantity,
    ParsedPackageType? PackageType,
    ParsedVariant? Variant);
