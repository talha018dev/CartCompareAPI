using CartCompareAPI.Canonicalization.Brands;
using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CartCompareAPI.Tests.Canonicalization.Brands;

public sealed class DatabaseBrandDefinitionProviderTests
{
    [Fact]
    public async Task GetAllAsync_WithNoConfiguredAliases_ShouldReturnDatabaseBrand()
    {
        await using var db = CreateDbContext();
        db.Brands.Add(new Brand { Name = "Marks", Slug = "marks" });
        await db.SaveChangesAsync();
        var provider = CreateProvider(db);

        BrandDefinition definition = Assert.Single(await provider.GetAllAsync());

        Assert.Equal("marks", definition.Key);
        Assert.Equal("Marks", definition.DisplayName);
        Assert.Empty(definition.Aliases);
    }

    [Fact]
    public async Task GetAllAsync_WithConfiguredAliases_ShouldMergeAliasesIntoBrand()
    {
        await using var db = CreateDbContext();
        db.Brands.Add(new Brand { Name = "Marks", Slug = "marks" });
        await db.SaveChangesAsync();
        var provider = CreateProvider(
            db,
            new BrandAliasDefinition("marks", ["mark's", "marks brand"]));

        BrandDefinition definition = Assert.Single(await provider.GetAllAsync());

        Assert.Equal(["mark's", "marks brand"], definition.Aliases);
    }

    [Fact]
    public async Task GetAllAsync_ShouldMatchConfiguredBrandKeyCaseInsensitively()
    {
        await using var db = CreateDbContext();
        db.Brands.Add(new Brand { Name = "Farm Fresh", Slug = "farm-fresh" });
        await db.SaveChangesAsync();
        var provider = CreateProvider(
            db,
            new BrandAliasDefinition("FARM-FRESH", ["farmfresh"]));

        BrandDefinition definition = Assert.Single(await provider.GetAllAsync());

        Assert.Equal(["farmfresh"], definition.Aliases);
    }

    [Fact]
    public async Task GetAllAsync_WithUnknownConfiguredBrandKey_ShouldThrow()
    {
        await using var db = CreateDbContext();
        db.Brands.Add(new Brand { Name = "Marks", Slug = "marks" });
        await db.SaveChangesAsync();
        var provider = CreateProvider(
            db,
            new BrandAliasDefinition("unknown", ["unknown brand"]));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetAllAsync());

        Assert.Contains("unknown", exception.Message);
    }

    [Fact]
    public async Task GetAllAsync_ShouldOrderDefinitionsByBrandSlug()
    {
        await using var db = CreateDbContext();
        db.Brands.AddRange(
            new Brand { Name = "Marks", Slug = "marks" },
            new Brand { Name = "Arla", Slug = "arla" });
        await db.SaveChangesAsync();
        var provider = CreateProvider(db);

        IReadOnlyCollection<BrandDefinition> definitions =
            await provider.GetAllAsync();

        Assert.Equal(["arla", "marks"], definitions.Select(x => x.Key));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static DatabaseBrandDefinitionProvider CreateProvider(
        AppDbContext db,
        params BrandAliasDefinition[] aliasDefinitions)
    {
        var options = Options.Create(new BrandCanonicalizationOptions
        {
            BrandAliases = [.. aliasDefinitions]
        });

        return new DatabaseBrandDefinitionProvider(db, options);
    }
}
