using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Canonicalization.Names;
using CartCompareAPI.Canonicalization.Packaging;
using CartCompareAPI.Canonicalization.Products;
using CartCompareAPI.Canonicalization.Quantity;
using CartCompareAPI.Canonicalization.Variants;

namespace CartCompareAPI.Canonicalization;

public static class CanonicalizationServiceCollectionExtensions
{
    public static IServiceCollection AddCanonicalization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<BrandCanonicalizationOptions>()
            .Bind(configuration.GetSection(
                BrandCanonicalizationOptions.SectionName))
            .Validate(
                options => options.BrandAliases.All(IsValidAliasDefinition),
                "Each brand alias definition requires a brand key and at least one nonblank alias.")
            .Validate(
                options => options.BrandAliases
                    .Select(definition => definition.BrandKey)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() == options.BrandAliases.Count,
                "Brand alias keys must be unique.")
            .ValidateOnStart();

        services.AddSingleton<IProductNameNormalizer, ProductNameNormalizer>();
        services.AddSingleton<IBrandResolver, BrandResolver>();
        services.AddSingleton<IQuantityParser, ProductQuantityParser>();
        services.AddSingleton<IPackageTypeParser, ProductPackageTypeParser>();
        services.AddSingleton<INormalizedProductNameBuilder, NormalizedProductNameBuilder>();
        services.AddSingleton<IVariantParser, ProductVariantParser>();
        services.AddSingleton<IProductNormalizationService, ProductNormalizationService>();
        services.AddSingleton<ICanonicalKeyBuilder, CanonicalKeyBuilder>();

        return services;
    }

    private static bool IsValidAliasDefinition(
        BrandAliasDefinition definition)
    {
        return !string.IsNullOrWhiteSpace(definition.BrandKey)
            && definition.Aliases.Count > 0
            && definition.Aliases.All(
                alias => !string.IsNullOrWhiteSpace(alias));
    }
}
