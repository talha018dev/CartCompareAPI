using System;
using CartCompareApi.Domain.Entities;
using CartCompareApi.Ingestion.Shwapno.Entities;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Ingestion.Shwapno.Import;

public class ShwapnoProductMapper()
{
    // public async Task<Dictionary<string, StoreProduct>> GetExistingProductsAsync()
    // {
    //     var existingProducts = await db.StoreProducts
    //     .Where(p => p.Store.Slug == "shwapno")
    //     .Include(p => p.Product)
    //     .ToDictionaryAsync(x => x.ExternalProductId, cancellationToken: default);

    //     return existingProducts;
    // }

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

    public StoreProduct Create(ShwapnoProduct source, Category category, Store store, DateTime now)
    {

        var product = new Product
        {
            CategoryId = category.Id,
            Name = source.Name.Trim(),
            NormalizedName = source.Name.Trim().ToLowerInvariant(),
            Quantity = source.OrderPackageQuantity,
            Unit = source.Unit.Trim(),
            ImageUrl = GetImageUrl(source),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var storeProduct = new StoreProduct
        {
            StoreId = store.Id,
            Product = product,
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
