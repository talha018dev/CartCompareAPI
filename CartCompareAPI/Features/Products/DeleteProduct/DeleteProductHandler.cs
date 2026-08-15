using CartCompareAPI.Features.Shared;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Features.Products.DeleteProduct;

public sealed class DeleteProductHandler(AppDbContext db)
{
    public async Task<CrudResult> Handle(Guid id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return CrudResult.NotFound();
        if (await db.StoreProducts.AnyAsync(x => x.ProductId == id))
            return CrudResult.Conflict("This product cannot be deleted because store listings reference it.");
        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return CrudResult.Success();
    }
}
