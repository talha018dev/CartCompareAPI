namespace CartCompareAPI.Features.Products.GetProductById;

public sealed record GetProductByIdResponse(Guid Id, Guid CategoryId, Guid? BrandId,
    string Name, string NormalizedName, decimal Quantity, string Unit, string? ImageUrl,
    bool IsActive, string Category, string? Brand);
