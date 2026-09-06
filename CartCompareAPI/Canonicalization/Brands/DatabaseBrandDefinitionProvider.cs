using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CartCompareAPI.Canonicalization.Brands;

public sealed class DatabaseBrandDefinitionProvider(
    AppDbContext db,
    IOptions<BrandCanonicalizationOptions> options)
    : IBrandDefinitionProvider
{
    public async Task<IReadOnlyCollection<BrandDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var brands = await db.Brands
            .AsNoTracking()
            .OrderBy(brand => brand.Slug)
            .ToListAsync(cancellationToken);

        var brandsBySlug = brands.ToDictionary(
            brand => brand.Slug,
            StringComparer.OrdinalIgnoreCase);

        var aliasesByBrandKey = options.Value.BrandAliases.ToDictionary(
            definition => definition.BrandKey,
            definition => definition.Aliases,
            StringComparer.OrdinalIgnoreCase);

        var unknownBrandKeys = aliasesByBrandKey.Keys
            .Where(brandKey => !brandsBySlug.ContainsKey(brandKey))
            .OrderBy(brandKey => brandKey, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (unknownBrandKeys.Length > 0)
        {
            throw new InvalidOperationException(
                $"Brand alias configuration references unknown brand slug(s): " +
                string.Join(", ", unknownBrandKeys));
        }

        return brands
            .Select(brand => new BrandDefinition(
                brand.Slug,
                brand.Name,
                aliasesByBrandKey.GetValueOrDefault(brand.Slug) ?? []))
            .ToArray();
    }
}
