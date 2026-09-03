using System.Text.Json;
using CartCompareAPI.Ingestion.Shwapno.Entities;
using Microsoft.Playwright;

namespace CartCompareAPI.Ingestion.Shwapno.Browser;

public class ShwapnoBrowserClient(IWebHostEnvironment _environment)
{
    public async Task GetProductsFromShwapno(string category)
    {
        var categorySlug = category.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(categorySlug) ||
            categorySlug.Any(character => !char.IsLetterOrDigit(character) && character != '-'))
        {
            throw new ArgumentException(
                "Category must contain only letters, numbers, and hyphens.",
                nameof(category));
        }

        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions
            {
                Headless = false
            });

        var page = await browser.NewPageAsync();

        var allProducts = new Dictionary<string, ShwapnoProduct>();

        var hasNextPage = true;
        var totalItems = int.MaxValue;

        var previousCount = 0;
        var stalledScrolls = 0;

        page.Response += async (_, response) =>
        {
            if (!response.Url.Contains("/api/category/products"))
                return;

            try
            {
                var json = await response.TextAsync();

                var result =
                    JsonSerializer.Deserialize<ShwapnoProductResponse>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (result?.Products == null)
                    return;

                totalItems = result.TotalItems;
                hasNextPage = result.HasNextPage;

                foreach (var product in result.Products)
                {
                    if (!string.IsNullOrWhiteSpace(product.Sku))
                    {
                        allProducts[product.Sku] = product;
                    }
                }

                Console.WriteLine(
                    $"API response: {result.Products.Count} products | " +
                    $"Total unique: {allProducts.Count}/{totalItems} | " +
                    $"Has next page: {hasNextPage}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error processing product response: {ex.Message}");
            }
        };

        Console.WriteLine($"Opening Shwapno {categorySlug} page...");

        await page.GotoAsync(
            $"https://www.shwapno.com/{categorySlug}",
            new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

        // Give the initial API request time to complete.
        await page.WaitForTimeoutAsync(2000);

        while (hasNextPage && allProducts.Count < totalItems)
        {
            previousCount = allProducts.Count;

            await page.Mouse.WheelAsync(0, 3000);

            // Give Shwapno time to request and render the next products.
            await page.WaitForTimeoutAsync(2000);

            if (allProducts.Count == previousCount)
            {
                stalledScrolls++;

                Console.WriteLine(
                    $"No new products loaded. " +
                    $"Stalled: {stalledScrolls}/3");
            }
            else
            {
                stalledScrolls = 0;
            }

            // Safety mechanism in case the page stops loading products.
            if (stalledScrolls >= 3)
            {
                Console.WriteLine(
                    "No new products loaded after 3 attempts. Stopping.");

                break;
            }
        }

        // Give the last response a little time to finish processing.
        await page.WaitForTimeoutAsync(2000);

        Console.WriteLine();
        Console.WriteLine("Finished collecting products.");
        Console.WriteLine($"Products collected: {allProducts.Count}");
        Console.WriteLine($"Expected products: {totalItems}");

        // Convert dictionary to list.
        var products = allProducts.Values.ToList();

        // Save JSON.
        var outputPath = Path.Combine(
            _environment.ContentRootPath,
            "Ingestion",
            "Shwapno",
            "Data",
            $"{categorySlug}.json"
        );

        Directory.CreateDirectory(
            Path.GetDirectoryName(outputPath)!);

        var outputJson = JsonSerializer.Serialize(
            products,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        await File.WriteAllTextAsync(
            outputPath,
            outputJson);

        Console.WriteLine();
        Console.WriteLine($"Saved {products.Count} products.");
        Console.WriteLine($"File: {outputPath}");

        // Keep browser open for a few seconds so you can see the result.
        await page.WaitForTimeoutAsync(3000);
    }
}
