using System.Text.Json;
using CartCompareAPI.Infrastructure.Data;
using CartCompareAPI.Ingestion.Shwapno;
using CartCompareAPI.Ingestion.Shwapno.Entities;
using CartCompareAPI.Ingestion.Shwapno.Import;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.FileProviders;

namespace CartCompareAPI.Tests.Ingestion.Shwapno;

public sealed class ShwapnoDairyImporterTests
{
    [Fact]
    public async Task ImportAsync_ShouldPersistUnlinkedListingAndHistoryWithoutProduct()
    {
        string contentRoot = Path.Combine(
            Path.GetTempPath(),
            $"cart-compare-tests-{Guid.NewGuid():N}");

        try
        {
            string dataDirectory = Path.Combine(
                contentRoot,
                "Ingestion",
                "Shwapno",
                "Data");
            Directory.CreateDirectory(dataDirectory);

            var source = new ShwapnoProduct
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

            await File.WriteAllTextAsync(
                Path.Combine(dataDirectory, "dairy.json"),
                JsonSerializer.Serialize(new[] { source }));

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings => warnings.Ignore(
                    InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            await using var db = new AppDbContext(options);
            var environment = new TestWebHostEnvironment(contentRoot);
            var importer = new ShwapnoDairyImporter(
                db,
                new ShwapnoJsonReader(environment),
                new ShwapnoCatalogInitializer(db),
                new ShwapnoProductMapper());

            await importer.ImportAsync();

            var listing = await db.StoreProducts
                .Include(product => product.PriceHistory)
                .SingleAsync();

            Assert.Null(listing.ProductId);
            Assert.Single(listing.PriceHistory);
            Assert.Empty(await db.Products.ToListAsync());
        }
        finally
        {
            if (Directory.Exists(contentRoot))
            {
                Directory.Delete(contentRoot, recursive: true);
            }
        }
    }

    private sealed class TestWebHostEnvironment(string contentRootPath)
        : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "CartCompareAPI.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = contentRootPath;
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; }
            = new PhysicalFileProvider(contentRootPath);
    }
}
