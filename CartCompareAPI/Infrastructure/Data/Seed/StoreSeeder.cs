using CartCompareApi.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;

namespace CartCompareApi.Infrastructure.Data.Seed;

public static class StoreSeeder
{
    public static class StoreSlugs
    {
        public const string Shwapno = "shwapno";
        public const string Agora = "agora";
        public const string MeenaBazar = "meena-bazar";
    }
    public static List<Store> Seed(AppDbContext db)
    {
        var stores = new List<Store>
        {
            new()
            {
                Name = "Shwapno",
                Slug = StoreSlugs.Shwapno,
                WebsiteUrl = "https://www.shwapno.com",
                IsActive = true
            },
            new()
            {
                Name = "Agora",
                Slug = StoreSlugs.Agora,
                WebsiteUrl = "https://www.agorasuperstores.com",
                IsActive = true
            },
            new()
            {
                Name = "Meena Bazar",
                Slug = StoreSlugs.MeenaBazar,
                WebsiteUrl = "https://www.meenabazaronline.com",
                IsActive = true
            }
        };

        db.Stores.AddRange(stores);
        db.SaveChanges();

        return stores;
    }
}