// Domain/Entities/Brand.cs
namespace CartCompareAPI.Domain.Entities;

public class Brand
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = [];
}
