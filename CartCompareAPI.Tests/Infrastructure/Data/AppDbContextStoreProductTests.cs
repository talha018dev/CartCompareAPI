using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Tests.Infrastructure.Data;

public sealed class AppDbContextStoreProductTests
{
    [Fact]
    public async Task SaveChanges_ShouldPersistStoreProductWithoutProduct()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var storeId = Guid.NewGuid();
        var storeProductId = Guid.NewGuid();

        await using (var db = new AppDbContext(options))
        {
            db.Stores.Add(new Store
            {
                Id = storeId,
                Name = "Shwapno",
                Slug = "shwapno",
                IsActive = true
            });
            db.StoreProducts.Add(new StoreProduct
            {
                Id = storeProductId,
                StoreId = storeId,
                ProductId = null,
                ExternalProductId = "SKU-1",
                StoreProductName = "Fresh Milk 500 ml",
                Price = 95m,
                InStock = true,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        await using (var db = new AppDbContext(options))
        {
            StoreProduct persisted = await db.StoreProducts.SingleAsync(
                product => product.Id == storeProductId);

            Assert.Null(persisted.ProductId);
            Assert.Null(persisted.Product);
            Assert.Empty(db.Products);
        }
    }
}
