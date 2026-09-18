using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Features.Products;
using CartCompareAPI.Features.Shared;
using CartCompareAPI.Infrastructure.Data;

namespace CartCompareAPI.Features.Products.EditProduct;

public sealed class EditProductHandler(AppDbContext db)
{
    public async Task<CrudResult> Handle(Guid id, EditProductRequest request)
    {
        Product? product = await db.Products.FindAsync(id);
        if (product is null)
        {
            return CrudResult.NotFound();
        }

        string? error = await ProductRequestValidator.Validate(db, request.CategoryId, request.BrandId, request.Name, request.Unit, request.Quantity);
        if (error is not null)
        {
            return CrudResult.Invalid(error);
        }

        product.CategoryId = request.CategoryId;
        product.BrandId = request.BrandId;
        product.Name = request.Name.Trim();
        product.NormalizedName = ProductRequestValidator.NormaliseName(request.Name, request.NormalizedName);
        product.Quantity = request.Quantity;
        product.Unit = request.Unit.Trim();
        product.ImageUrl = request.ImageUrl?.Trim();
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return CrudResult.Success();
    }
}
