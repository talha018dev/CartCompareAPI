using CartCompareAPI.Canonicalization;
using CartCompareAPI.Canonicalization.Brands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CartCompareAPI.Tests.Canonicalization.Brands;

public sealed class BrandCanonicalizationOptionsTests
{
    [Fact]
    public void AddCanonicalization_ShouldBindBrandAliases()
    {
        IConfiguration configuration = BuildConfiguration(new()
        {
            ["Canonicalization:BrandAliases:0:BrandKey"] = "marks",
            ["Canonicalization:BrandAliases:0:Aliases:0"] = "mark's"
        });

        var services = new ServiceCollection();
        services.AddCanonicalization(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        BrandCanonicalizationOptions options = provider
            .GetRequiredService<IOptions<BrandCanonicalizationOptions>>()
            .Value;

        BrandAliasDefinition definition = Assert.Single(
            options.BrandAliases);
        Assert.Equal("marks", definition.BrandKey);
        Assert.Equal(["mark's"], definition.Aliases);
    }

    [Fact]
    public void AddCanonicalization_WithDuplicateKeys_ShouldFailValidation()
    {
        IConfiguration configuration = BuildConfiguration(new()
        {
            ["Canonicalization:BrandAliases:0:BrandKey"] = "marks",
            ["Canonicalization:BrandAliases:0:Aliases:0"] = "mark's",
            ["Canonicalization:BrandAliases:1:BrandKey"] = "MARKS",
            ["Canonicalization:BrandAliases:1:Aliases:0"] = "marks brand"
        });

        var services = new ServiceCollection();
        services.AddCanonicalization(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<BrandCanonicalizationOptions> options = provider
            .GetRequiredService<IOptions<BrandCanonicalizationOptions>>();

        Assert.Throws<OptionsValidationException>(() => _ = options.Value);
    }

    [Fact]
    public void AddCanonicalization_WithNoAliases_ShouldBeValid()
    {
        IConfiguration configuration = BuildConfiguration([]);

        var services = new ServiceCollection();
        services.AddCanonicalization(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        BrandCanonicalizationOptions options = provider
            .GetRequiredService<IOptions<BrandCanonicalizationOptions>>()
            .Value;

        Assert.Empty(options.BrandAliases);
    }

    private static IConfiguration BuildConfiguration(
        Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
