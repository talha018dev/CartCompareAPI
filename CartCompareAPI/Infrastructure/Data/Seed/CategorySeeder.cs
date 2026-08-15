using System;
using CartCompareApi.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;

namespace CartCompareAPI.Infrastructure.Seed;

public class CategorySeeder
{

    public static class CategorySlugs
    {
        public const string Beverages = "beverages";
        public const string Dairy = "dairy";
        public const string Snacks = "snacks";
        public const string Rice = "rice";
        public const string CookingOil = "cooking-oil";
    }
    public static List<Category> Seed(AppDbContext db)
    {
        var categories = new List<Category>
        {
            new()
            {
                Name = "Beverages",
                Slug = CategorySlugs.Beverages
            },
            new()
            {
                Name = "Dairy",
                Slug = CategorySlugs.Dairy
            },
            new()
            {
                Name = "Snacks",
                Slug = CategorySlugs.Snacks
            },
            new()
            {
                Name = "Rice",
                Slug = CategorySlugs.Rice
            },
            new()
            {
                Name = "Cooking Oil",
                Slug = CategorySlugs.CookingOil
            }
        };

        db.Categories.AddRange(categories);
        db.SaveChanges();

        return categories;


    }
}
