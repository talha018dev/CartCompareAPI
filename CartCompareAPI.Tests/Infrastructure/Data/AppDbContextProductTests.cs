using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Tests.Infrastructure.Data;

public sealed class AppDbContextProductTests
{
    [Fact]
    public async Task SaveChanges_ShouldPersistCanonicalProductFields()
    {
        var options = CreateOptions();
        var productId = Guid.NewGuid();

        await using (var db = new AppDbContext(options))
        {
            Product product = CreateProduct(productId);
            product.CanonicalKey = "dairy|marks|full cream milk powder|full cream|1000-g|tin";
            product.Variant = "chocolate+full cream";
            product.PackageType = "tin";

            db.Products.Add(product);
            await db.SaveChangesAsync();
        }

        await using (var db = new AppDbContext(options))
        {
            Product persisted = await db.Products.SingleAsync(
                product => product.Id == productId);

            Assert.Equal(
                "dairy|marks|full cream milk powder|full cream|1000-g|tin",
                persisted.CanonicalKey);
            Assert.Equal("chocolate+full cream", persisted.Variant);
            Assert.Equal("tin", persisted.PackageType);
        }
    }

    [Fact]
    public async Task SaveChanges_ShouldPermitNullCanonicalProductFields()
    {
        var options = CreateOptions();
        var productId = Guid.NewGuid();

        await using (var db = new AppDbContext(options))
        {
            Product product = CreateProduct(productId);
            product.CanonicalKey = null;
            product.Variant = null;
            product.PackageType = null;

            db.Products.Add(product);
            await db.SaveChangesAsync();
        }

        await using (var db = new AppDbContext(options))
        {
            Product persisted = await db.Products.SingleAsync(
                product => product.Id == productId);

            Assert.Null(persisted.CanonicalKey);
            Assert.Null(persisted.Variant);
            Assert.Null(persisted.PackageType);
        }
    }

    [Fact]
    public void Model_ShouldDefineUniqueCanonicalKeyIndex()
    {
        var options = CreateOptions();
        using var db = new AppDbContext(options);

        var productType = db.Model.FindEntityType(typeof(Product));
        var index = Assert.Single(
            productType!.GetIndexes(),
            candidate =>
                candidate.Properties.Count == 1 &&
                candidate.Properties[0].Name == nameof(Product.CanonicalKey));

        Assert.True(index.IsUnique);
    }

    private static DbContextOptions<AppDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private static Product CreateProduct(Guid id)
    {
        return new Product
        {
            Id = id,
            CategoryId = Guid.NewGuid(),
            Name = "Marks Full Cream Milk Powder 1kg",
            NormalizedName = "full cream milk powder",
            Quantity = 1000m,
            Unit = "g",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

}
