using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Features.Products.GetProductById;

public sealed class GetProductByIdHandler(AppDbContext db)
{
    public Task<GetProductByIdResponse?> Handle(Guid id) => db.Products.AsNoTracking()
        .Where(x => x.Id == id)
        .Select(x => new GetProductByIdResponse
        {
            Id = x.Id,
            CategoryId = x.CategoryId,
            BrandId = x.BrandId,
            Name = x.Name,
            NormalizedName = x.NormalizedName,
            Quantity = x.Quantity,
            Unit = x.Unit,
            ImageUrl = x.ImageUrl,
            IsActive = x.IsActive,
            Category = x.Category.Name,
            Brand = x.Brand != null ? x.Brand.Name : null
        })
        .FirstOrDefaultAsync();
}
