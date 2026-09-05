using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Canonicalization.Names;
using CartCompareAPI.Canonicalization.Packaging;
using CartCompareAPI.Canonicalization.Quantity;
using CartCompareAPI.Canonicalization.Variants;

namespace CartCompareAPI.Canonicalization.Products;

public sealed class ProductNormalizationService(
    IBrandResolver brandResolver,
    IQuantityParser quantityParser,
    IPackageTypeParser packageTypeParser,
    INormalizedProductNameBuilder normalizedNameBuilder,
    IVariantParser variantParser)
    : IProductNormalizationService
{
    public ProductNormalizationResult Normalize(
        string productName,
        IReadOnlyCollection<BrandDefinition> brands)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return ProductNormalizationResult.Unresolved(
                ProductNormalizationFailure.MissingName);
        }

        BrandResolution? brand = brandResolver.Resolve(productName, brands);
        if (brand is null)
        {
            return ProductNormalizationResult.Unresolved(
                ProductNormalizationFailure.BrandNotResolved);
        }

        ParsedQuantity? quantity = quantityParser.Parse(productName);
        if (quantity is null)
        {
            return ProductNormalizationResult.Unresolved(
                ProductNormalizationFailure.QuantityNotResolved);
        }

        ParsedPackageType? packageType = packageTypeParser.Parse(productName);
        string normalizedName = normalizedNameBuilder.Build(
            productName,
            brand,
            quantity,
            packageType);

        if (normalizedName.Length == 0)
        {
            return ProductNormalizationResult.Unresolved(
                ProductNormalizationFailure.NormalizedNameEmpty);
        }

        ParsedVariant? variant = variantParser.Parse(normalizedName);

        return ProductNormalizationResult.Success(new NormalizedProduct(
            productName.Trim(),
            normalizedName,
            brand,
            quantity,
            packageType,
            variant));
    }
}
