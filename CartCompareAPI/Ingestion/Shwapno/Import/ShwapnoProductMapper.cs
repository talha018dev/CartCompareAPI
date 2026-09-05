using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Ingestion.Shwapno.Entities;

namespace CartCompareAPI.Ingestion.Shwapno.Import;

public sealed class ShwapnoProductMapper
{
    public void Update(StoreProduct storeProduct, ShwapnoProduct source, DateTime now)
    {
        storeProduct.StoreProductName = source.Name;
        storeProduct.Price = source.Price.PriceValue;
        storeProduct.OriginalPrice = source.Price.oldPriceValue;
        storeProduct.InStock = IsInStock(source);
        storeProduct.ProductUrl = GetProductUrl(source);
        storeProduct.ImageUrl = GetImageUrl(source);
        storeProduct.LastUpdated = now;
    }

    public StoreProduct Create(ShwapnoProduct source, Store store, DateTime now)
    {
        var storeProduct = new StoreProduct
        {
            StoreId = store.Id,
            ProductId = null,
            ExternalProductId = source.Sku,
            StoreProductName = source.Name,
            Price = source.Price.PriceValue,
            OriginalPrice = source.Price.oldPriceValue,
            InStock = IsInStock(source),
            ProductUrl = GetProductUrl(source),
            ImageUrl = GetImageUrl(source),
            LastUpdated = now,
            CreatedAt = now
        };

        storeProduct.PriceHistory.Add(new PriceHistory
        {
            Price = storeProduct.Price,
            OriginalPrice = storeProduct.OriginalPrice,
            InStock = storeProduct.InStock,
            RecordedAt = now
        });

        return storeProduct;
    }

    private static bool IsInStock(ShwapnoProduct source)
    {
        return source.Stock.Equals(
            "InStock",
            StringComparison.OrdinalIgnoreCase)
               && source.Status.Equals(
                   "Available",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string GetProductUrl(ShwapnoProduct source)
    {
        return $"https://www.shwapno.com/{source.SeName.TrimStart('/')}";
    }

    private static string? GetImageUrl(ShwapnoProduct source)
    {
        return source.Picture?
            .LargeDeviceUrl?
            .FullSizeImageUrl;
    }

}
