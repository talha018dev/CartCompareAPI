using CartCompareApi.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;

namespace CartCompareApi.Infrastructure.Data.Seed;

public  class BrandSeeder
{

    public  class BrandSlugs
    {
        public const string Pran = "pran";
        public const string Fresh = "fresh";
        public const string Radhuni = "radhuni";
        public const string Aarong = "aarong";
        // public const string CocaCola = "coca-cola";
    }
    public static List<Brand> Seed(AppDbContext db)
    {


        var brands = new List<Brand>
        {
            new()
            {
                Name = "Pran",
                Slug = BrandSlugs.Pran
            },
            new()
            {
                Name = "Aarong",
                Slug = BrandSlugs.Aarong
            },
            new()
            {
                Name = "Fresh",
                Slug = BrandSlugs.Fresh
            },
            new()
            {
                Name = "Radhuni",
                Slug = BrandSlugs.Radhuni
            }
        };

        db.Brands.AddRange(brands);
        db.SaveChanges();

        return brands;
    }
}