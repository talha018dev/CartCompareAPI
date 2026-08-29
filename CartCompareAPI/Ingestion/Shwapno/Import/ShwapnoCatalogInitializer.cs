using System;
using CartCompareApi.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Ingestion.Shwapno.Import;

public class ShwapnoCatalogInitializer(AppDbContext db)
{

    public async Task<ShwapnoCatalog> ShwapnoCatalogInitializedAsync()
    {
        var category = await db.Categories.SingleOrDefaultAsync(x => x.Slug == "dairy", cancellationToken: default);
        var store = await db.Stores.SingleOrDefaultAsync(x => x.Slug == "shwapno", cancellationToken: default);

        if (category is null)
        {
            category = new Category { Name = "Dairy", Slug = "dairy" };
            db.Categories.Add(category);
        }
        if (store is null)
        {
            store = new Store { Name = "Shwapno", Slug = "shwapno" };
            db.Stores.Add(store);
        }


        await db.SaveChangesAsync(cancellationToken: default);

        return new ShwapnoCatalog(category, store);
    }
}
