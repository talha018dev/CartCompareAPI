using CartCompareApi.Domain.Entities;
using CartCompareAPI.Features.Products;
using CartCompareAPI.Features.Shared;
using CartCompareAPI.Infrastructure.Data;

namespace CartCompareAPI.Features.Products.CreateProduct;

public sealed class CreateProductHandler(AppDbContext db)
{
    public async Task<CrudResult<CreateProductResponse>> Handle(CreateProductRequest request)
    {
        var error = await ProductRequestValidator.Validate(db, request.CategoryId, request.BrandId, request.Name, request.Unit, request.Quantity);
        if (error is not null) return CrudResult<CreateProductResponse>.Invalid(error);

        var product = new Product
        {
            Id = Guid.NewGuid(), CategoryId = request.CategoryId, BrandId = request.BrandId,
            Name = request.Name.Trim(), NormalizedName = ProductRequestValidator.NormaliseName(request.Name, request.NormalizedName),
            Quantity = request.Quantity, Unit = request.Unit.Trim(), ImageUrl = request.ImageUrl?.Trim(),
            IsActive = request.IsActive, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return CrudResult<CreateProductResponse>.Success(new(product.Id));
    }
}
