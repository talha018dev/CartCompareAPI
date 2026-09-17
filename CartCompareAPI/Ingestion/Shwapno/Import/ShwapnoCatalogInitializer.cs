using System;
using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Ingestion.Shwapno.Import;

public class ShwapnoCatalogInitializer(AppDbContext db)
{

    public async Task<ShwapnoCatalog> ShwapnoCatalogInitializedAsync(string categorySlug,
    CancellationToken cancellationToken = default)
    {
        Category? category = await db.Categories.SingleOrDefaultAsync(x => x.Slug == categorySlug, cancellationToken);
        Store? store = await db.Stores.SingleOrDefaultAsync(x => x.Slug == "shwapno", cancellationToken);

        if (category is null)
        {
            category = new Category
            {
                Name = categorySlug.Replace('-', ' '),
                Slug = categorySlug
            };
            db.Categories.Add(category);
        }
        if (store is null)
        {
            store = new Store { Name = "Shwapno", Slug = "shwapno" };
            db.Stores.Add(store);
        }


        await db.SaveChangesAsync(cancellationToken);

        return new ShwapnoCatalog(category, store);
    }
}
