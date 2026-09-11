using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CartCompareAPI.Canonicalization.Brands;

public sealed class BrandCatalogInitializer(
    AppDbContext db,
    IOptions<BrandCanonicalizationOptions> options)
{
    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        List<string> existingSlugs = await db.Brands
            .Select(brand => brand.Slug)
            .ToListAsync(cancellationToken);

        var knownSlugs = existingSlugs.ToHashSet(
            StringComparer.OrdinalIgnoreCase);

        foreach (BrandAliasDefinition definition in
                 options.Value.BrandAliases)
        {
            string slug = definition.BrandKey.Trim().ToLowerInvariant();

            if (!knownSlugs.Add(slug))
            {
                continue;
            }

            db.Brands.Add(new Brand
            {
                Id = Guid.NewGuid(),
                Name = definition.DisplayName.Trim(),
                Slug = slug
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
