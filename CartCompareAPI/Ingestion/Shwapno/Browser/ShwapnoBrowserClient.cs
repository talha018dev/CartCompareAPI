using System.Text.Json;
using CartCompareAPI.Ingestion.Shwapno.Entities;
using Microsoft.Playwright;

namespace CartCompareAPI.Ingestion.Shwapno.Browser;

public class ShwapnoBrowserClient(IConfiguration configuration)
{
    public async Task<IReadOnlyCollection<ShwapnoProduct>> GetProductsFromShwapno(
        string category,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var categorySlug = category.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(categorySlug) ||
            categorySlug.Any(character => !char.IsLetterOrDigit(character) && character != '-'))
        {
            throw new ArgumentException(
                "Category must contain only letters, numbers, and hyphens.",
                nameof(category));
        }

        using var playwright = await Playwright.CreateAsync();

        cancellationToken.ThrowIfCancellationRequested();
        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = configuration.GetValue<bool>("Ingestion:Headless") });

        var page = await browser.NewPageAsync();
        var allProducts = new Dictionary<string, ShwapnoProduct>();
        var responseTasks = new List<Task>();
        var responseLock = new object();
        var hasNextPage = true;
        int? totalItems = null;
        var productResponseCount = 0;
        var stalledScrolls = 0;

        void OnResponse(object? sender, IResponse response)
        {
            if (!response.Url.Contains("/api/category/products", StringComparison.OrdinalIgnoreCase))
                return;

            lock (responseLock)
            {
                responseTasks.Add(ProcessProductResponseAsync(response));
            }
        }

        async Task ProcessProductResponseAsync(IResponse response)
        {
            try
            {
                var json = await response.TextAsync();
                var result = JsonSerializer.Deserialize<ShwapnoProductResponse>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Products == null)
                    return;

                lock (responseLock)
                {
                    totalItems = result.TotalItems;
                    hasNextPage = result.HasNextPage;
                    productResponseCount++;

                    foreach (var product in result.Products)
                    {
                        if (!string.IsNullOrWhiteSpace(product.Sku))
                            allProducts[product.Sku] = product;
                    }

                    Console.WriteLine(
                        $"API response: {result.Products.Count} products | " +
                        $"Total unique: {allProducts.Count}/{totalItems} | " +
                        $"Has next page: {hasNextPage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing product response: {ex.Message}");
            }
        }

        page.Response += OnResponse;

        Console.WriteLine($"Opening Shwapno {categorySlug} page...");
        cancellationToken.ThrowIfCancellationRequested();

        IResponse? navigationResponse;

        try
        {
            navigationResponse = await page.GotoAsync(
                $"https://www.shwapno.com/{categorySlug}",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        }
        catch (PlaywrightException ex)
        {
            Console.WriteLine($"Navigation failed: {ex.Message}");
            throw;
        }

        Console.WriteLine(
            $"Navigation status: {navigationResponse?.Status.ToString() ?? "unknown"}");

        // Allow the initial product request to finish before deciding whether to scroll.
        await Task.Delay(2000, cancellationToken);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int previousCount;
            lock (responseLock)
            {
                if (!hasNextPage || (totalItems is not null && allProducts.Count >= totalItems))
                    break;

                previousCount = allProducts.Count;
            }

            await page.Mouse.WheelAsync(0, 3000);
            await Task.Delay(2000, cancellationToken);

            lock (responseLock)
            {
                stalledScrolls = allProducts.Count == previousCount ? stalledScrolls + 1 : 0;
            }

            if (stalledScrolls > 0)
                Console.WriteLine($"No new products loaded. Stalled: {stalledScrolls}/3");

            if (stalledScrolls >= 3)
            {
                Console.WriteLine("No new products loaded after 3 attempts. Stopping.");
                break;
            }
        }

        // Stop accepting new response tasks, then wait for every captured response.
        await Task.Delay(2000, cancellationToken);
        page.Response -= OnResponse;

        Task[] pendingResponses;
        lock (responseLock)
        {
            pendingResponses = responseTasks.ToArray();
        }

        await Task.WhenAll(pendingResponses).WaitAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        lock (responseLock)
        {
            Console.WriteLine("Finished collecting products.");
            Console.WriteLine($"Captured product API responses: {productResponseCount}");
            Console.WriteLine($"Products collected: {allProducts.Count}");
            Console.WriteLine($"Expected products: {totalItems?.ToString() ?? "unknown"}");

            if (productResponseCount == 0)
                throw new InvalidOperationException("No Shwapno product API response was captured.");

            return allProducts.Values.ToArray();
        }
    }
}
