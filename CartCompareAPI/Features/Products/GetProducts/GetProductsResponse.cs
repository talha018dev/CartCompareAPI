namespace CartCompareAPI.Features.Products.GetProducts;

public class GetProductsResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public string Category { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }
}
