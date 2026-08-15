// Domain/Entities/StoreProduct.cs
namespace CartCompareApi.Domain.Entities;

public class StoreProduct
{
    public Guid Id { get; set; }

    public Guid StoreId { get; set; }
    public Guid ProductId { get; set; }

    public string ExternalProductId { get; set; } = string.Empty;
    public string StoreProductName { get; set; } = string.Empty;

    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }

    public bool InStock { get; set; }

    public string? ProductUrl { get; set; }
    public string? ImageUrl { get; set; }

    public DateTime LastUpdated { get; set; }
    public DateTime CreatedAt { get; set; }

    public Store Store { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public ICollection<PriceHistory> PriceHistory { get; set; } = [];
}