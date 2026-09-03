// Domain/Entities/Product.cs
namespace CartCompareAPI.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Category Category { get; set; } = null!;
    public Brand? Brand { get; set; }

    public ICollection<StoreProduct> StoreProducts { get; set; } = [];
}
