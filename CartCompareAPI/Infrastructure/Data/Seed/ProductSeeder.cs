using CartCompareApi.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;
using static CartCompareApi.Infrastructure.Data.Seed.BrandSeeder;
using static CartCompareAPI.Infrastructure.Seed.CategorySeeder;

namespace CartCompareApi.Infrastructure.Data.Seed;

public static class ProductSeeder
{
    public static List<Product> Seed(
        AppDbContext db,
        List<Category> categories,
        List<Brand> brands)
    {

        var beverages = categories.First(x => x.Slug == CategorySlugs.Beverages);
        var dairy = categories.First(x => x.Slug == CategorySlugs.Dairy);
        var snacks = categories.First(x => x.Slug == CategorySlugs.Snacks);
        var rice = categories.First(x => x.Slug == CategorySlugs.Rice);
        var cookingOil = categories.First(x => x.Slug == CategorySlugs.CookingOil);

        var pran = brands.First(x => x.Slug == BrandSlugs.Pran);
        var fresh = brands.First(x => x.Slug == BrandSlugs.Fresh);
        var radhuni = brands.First(x => x.Slug == BrandSlugs.Radhuni);

        var now = DateTime.UtcNow;

        var products = new List<Product>
        {
            new()
            {
                CategoryId = dairy.Id,
                BrandId = pran.Id,
                Name = "Pran UHT Milk 1L",
                NormalizedName = "pran uht milk 1l",
                Quantity = 1,
                Unit = "L",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                CategoryId = snacks.Id,
                BrandId = pran.Id,
                Name = "Pran Potato Crackers 100g",
                NormalizedName = "pran potato crackers 100g",
                Quantity = 100,
                Unit = "g",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                CategoryId = rice.Id,
                BrandId = fresh.Id,
                Name = "Fresh Nazirshail Rice 5kg",
                NormalizedName = "fresh nazirshail rice 5kg",
                Quantity = 5,
                Unit = "kg",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                CategoryId = cookingOil.Id,
                BrandId = radhuni.Id,
                Name = "Radhuni Soybean Oil 5L",
                NormalizedName = "radhuni soybean oil 5l",
                Quantity = 5,
                Unit = "L",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        db.Products.AddRange(products);
        db.SaveChanges();

        return products;
    }
}