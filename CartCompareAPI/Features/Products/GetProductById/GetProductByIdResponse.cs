namespace CartCompareAPI.Features.Products.GetProductById;

public sealed record GetProductByIdResponse()
{
    public Guid Id  {get; set;}
    public Guid CategoryId {get; set;}
    public Guid? BrandId {get; set;}
    public string Name {get;set;} = string.Empty;
    public string NormalizedName {get; set;} = string.Empty;
    public decimal Quantity {get; set;}
    public string Unit {get; set;} = string.Empty;
    public string? ImageUrl {get; set;}
    public bool IsActive {get; set;}
    public string Category {get; set;} = string.Empty;
    public string? Brand {get; set;}
}
