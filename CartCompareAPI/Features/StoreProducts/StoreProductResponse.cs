using System;

namespace CartCompareAPI.Features.StoreProducts;

public class StoreProductResponse
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string StoreSlug { get; set; } = string.Empty;
    public string ExternalProductId { get; set; } = string.Empty;
    public string StoreProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public bool InStock { get; set; }
    public string? ProductUrl { get; set; }
    public string? ImageUrl { get; set; }
}
