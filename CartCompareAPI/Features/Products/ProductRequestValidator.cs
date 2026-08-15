using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Features.Products;

internal static class ProductRequestValidator
{
    public static async Task<string?> Validate(AppDbContext db, Guid categoryId, Guid? brandId, string name, string unit, decimal quantity)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(unit) || quantity <= 0)
            return "Name and unit are required, and quantity must be greater than zero.";
        if (!await db.Categories.AnyAsync(x => x.Id == categoryId)) return "The selected category does not exist.";
        if (brandId.HasValue && !await db.Brands.AnyAsync(x => x.Id == brandId.Value)) return "The selected brand does not exist.";
        return null;
    }

    public static string NormaliseName(string name, string? normalizedName) =>
        string.IsNullOrWhiteSpace(normalizedName) ? name.Trim().ToLowerInvariant() : normalizedName.Trim();
}
