// Domain/Entities/PriceHistory.cs
namespace CartCompareApi.Domain.Entities;

public class PriceHistory
{
    public Guid Id { get; set; }

    public Guid StoreProductId { get; set; }

    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }

    public bool InStock { get; set; }

    public DateTime RecordedAt { get; set; }

    public StoreProduct StoreProduct { get; set; } = null!;
}