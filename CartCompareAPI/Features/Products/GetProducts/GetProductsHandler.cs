using CartCompareAPI.Features.StoreProducts;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Features.Products.GetProducts;

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
                ImageUrl = product.ImageUrl,
                StoreProducts = product.StoreProducts
                    .OrderBy(storeProduct => storeProduct.Price)
                    .Select(storeProduct => new StoreProductResponse
                    {
                        Id = storeProduct.Id,
                        StoreId = storeProduct.StoreId,
                        StoreName = storeProduct.Store.Name,
                        StoreSlug = storeProduct.Store.Slug,
                        ExternalProductId = storeProduct.ExternalProductId,
                        StoreProductName = storeProduct.StoreProductName,
                        Price = storeProduct.Price,
                        OriginalPrice = storeProduct.OriginalPrice,
                        InStock = storeProduct.InStock,
                        ProductUrl = storeProduct.ProductUrl,
                        ImageUrl = storeProduct.ImageUrl
                    })
                    .ToList()
            })
            .ToListAsync();
    }
}
