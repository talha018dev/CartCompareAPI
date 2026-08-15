using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Features.Products.GetProductById;

public sealed class GetProductByIdHandler(AppDbContext db)
{
    public Task<GetProductByIdResponse?> Handle(Guid id) => db.Products.AsNoTracking()
        .Where(x => x.Id == id)
        .Select(x => new GetProductByIdResponse(x.Id, x.CategoryId, x.BrandId, x.Name,
            x.NormalizedName, x.Quantity, x.Unit, x.ImageUrl, x.IsActive,
            x.Category.Name, x.Brand == null ? null : x.Brand.Name))
        .FirstOrDefaultAsync();
}
