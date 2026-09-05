using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Ingestion.Shwapno.Entities;
using CartCompareAPI.Ingestion.Shwapno.Import;

namespace CartCompareAPI.Tests.Ingestion.Shwapno;

public sealed class ShwapnoProductMapperTests
{
    private readonly ShwapnoProductMapper mapper = new();

    [Fact]
    public void Create_ShouldCreateUnlinkedStoreProductWithInitialPriceHistory()
    {
        var now = new DateTime(2026, 9, 5, 12, 30, 0, DateTimeKind.Utc);
        var store = new Store { Id = Guid.NewGuid(), Name = "Shwapno", Slug = "shwapno" };
        var source = CreateSource();

        StoreProduct result = mapper.Create(source, store, now);

        Assert.Null(result.ProductId);
        Assert.Null(result.Product);
        Assert.Equal(store.Id, result.StoreId);
        Assert.Equal(source.Sku, result.ExternalProductId);
        Assert.Equal(source.Name, result.StoreProductName);
        Assert.Equal(source.Price.PriceValue, result.Price);
        Assert.Equal(source.Price.oldPriceValue, result.OriginalPrice);
        Assert.True(result.InStock);
        Assert.Equal("https://www.shwapno.com/fresh-milk", result.ProductUrl);
        Assert.Equal("https://images.example/fresh-milk.jpg", result.ImageUrl);
        Assert.Equal(now, result.CreatedAt);
        Assert.Equal(now, result.LastUpdated);

        PriceHistory history = Assert.Single(result.PriceHistory);
        Assert.Equal(result.Price, history.Price);
        Assert.Equal(result.OriginalPrice, history.OriginalPrice);
        Assert.Equal(result.InStock, history.InStock);
        Assert.Equal(now, history.RecordedAt);
    }

    private static ShwapnoProduct CreateSource() => new()
    {
        Name = "Fresh Milk 500 ml",
        Sku = "SKU-1",
        SeName = "/fresh-milk",
        Stock = "InStock",
        Status = "Available",
        Price = new PriceClass
        {
            PriceValue = 95m,
            oldPriceValue = 100m
        },
        Picture = new Picture
        {
            LargeDeviceUrl = new LargeDeviceUrl
            {
                FullSizeImageUrl = "https://images.example/fresh-milk.jpg"
            }
        }
    };
}
