namespace CartCompareAPI.Features.Products.CreateProduct;

public sealed class CreateProductRequest
{
    public Guid CategoryId { get; init; }
    public Guid? BrandId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? NormalizedName { get; init; }
    public decimal Quantity { get; init; }
    public string Unit { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; } = true;
}
