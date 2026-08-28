// Domain/Entities/Store.cs
namespace CartCompareApi.Domain.Entities;

public class Store
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public bool IsActive { get; set; }

    public ICollection<StoreProduct> StoreProducts { get; set; } = [];
}