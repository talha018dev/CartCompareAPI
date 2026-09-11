using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CartCompareAPI.Tests.Canonicalization.Brands;

public sealed class BrandCatalogInitializerTests
{
    [Fact]
    public async Task InitializeAsync_ShouldCreateMissingConfiguredBrands()
    {
        await using AppDbContext db = CreateDbContext();
        var initializer = CreateInitializer(db);

        await initializer.InitializeAsync();

        Brand brand = await db.Brands.SingleAsync();
        Assert.Equal("marks", brand.Slug);
        Assert.Equal("Marks", brand.Name);
    }

    [Fact]
    public async Task InitializeAsync_WhenRunTwice_ShouldNotDuplicateBrands()
    {
        await using AppDbContext db = CreateDbContext();
        var initializer = CreateInitializer(db);

        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        Assert.Single(await db.Brands.ToListAsync());
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static BrandCatalogInitializer CreateInitializer(AppDbContext db)
    {
        var definition = new BrandAliasDefinition("marks", ["mark's"])
        {
            DisplayName = "Marks"
        };
        var options = Options.Create(new BrandCanonicalizationOptions
        {
            BrandAliases = [definition]
        });

        return new BrandCatalogInitializer(db, options);
    }
}
