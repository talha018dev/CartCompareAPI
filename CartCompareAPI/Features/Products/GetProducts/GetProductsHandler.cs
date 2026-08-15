using CartCompareApi.Infrastructure.Data;
using CartCompareAPI.Features.Products.GetProducts;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareApi.Features.Products.GetProducts;

public class GetProductsHandler(AppDbContext _db)
{
    
    public async Task<List<GetProductsResponse>> Handle(
        GetProductsRequest request)
    {
        return await _db.Products
            .AsNoTracking()
            .Select(product => new GetProductsResponse
            {
                Id = product.Id,
                Name = product.Name,
                Brand = product.Brand != null
                    ? product.Brand.Name
                    : null,
                Category = product.Category.Name,
                Quantity = product.Quantity,
                Unit = product.Unit,
                ImageUrl = product.ImageUrl
            })
            .ToListAsync();
    }
}