using CartCompareAPI.Canonicalization.StoreProducts;
using CartCompareAPI.Ingestion.Shwapno;
using CartCompareAPI.Ingestion.Shwapno.Browser;
using CartCompareAPI.Ingestion.Shwapno.Entities;
using CartCompareAPI.Ingestion.Shwapno.Import;
using Microsoft.Extensions.Logging;

namespace CartCompareAPI.Tests.Ingestion.Shwapno;

public sealed class ShwapnoIngestionOrchestratorTests
{
    [Fact]
    public async Task IngestAsync_ShouldPassScrapedProductsAndRunPhasesInOrder()
    {
        var calls = new List<string>();
        IReadOnlyCollection<ShwapnoProduct> products =
            [new() { Sku = "SKU-1" }, new() { Sku = "SKU-2" }];
        var importResult = new ShwapnoImportResult(
            new ShwapnoImportSummary(2, 1, 1), Guid.NewGuid(), Guid.NewGuid());
        var canonicalizationSummary = new StoreProductCanonicalizationSummary(1, 1, 0, 0);
        var source = new RecordingSource(calls, products);
        var importer = new RecordingImporter(calls, importResult);
        var canonicalization = new RecordingCanonicalizationService(
            calls, canonicalizationSummary);
        var logger = new RecordingLogger();
        var orchestrator = new ShwapnoIngestionOrchestrator(
            source, importer, canonicalization, logger);
        using var cancellation = new CancellationTokenSource();

        var result = await orchestrator.IngestAsync("dairy", cancellation.Token);

        Assert.Equal(new[] { "scrape", "import", "canonicalize" }, calls);
        Assert.Same(products, importer.ReceivedProducts);
        Assert.Equal("dairy", source.ReceivedCategory);
        Assert.Equal("dairy", importer.ReceivedCategory);
        Assert.Equal(importResult.StoreId, canonicalization.ReceivedStoreId);
        Assert.Equal(importResult.CategoryId, canonicalization.ReceivedCategoryId);
        Assert.Equal(cancellation.Token, source.ReceivedToken);
        Assert.Equal(cancellation.Token, importer.ReceivedToken);
        Assert.Equal(cancellation.Token, canonicalization.ReceivedToken);
        Assert.Equal(
            new ShwapnoIngestionResult(
                ShwapnoIngestionStatus.Completed,
                new ShwapnoScrapeSummary(2),
                importResult.Summary,
                new ShwapnoCanonicalizationResult(
                    true,
                    canonicalizationSummary,
                    null)),
            result);
        Assert.Null(logger.LastException);
    }

    [Fact]
    public async Task IngestAsync_ShouldNotImportOrCanonicalizeWhenScrapingFails()
    {
        var calls = new List<string>();
        var failure = new InvalidOperationException("Scraping failed.");
        var source = new RecordingSource(calls, []) { Failure = failure };
        var importer = new RecordingImporter(calls, ImportResult());
        var canonicalization = new RecordingCanonicalizationService(
            calls, new StoreProductCanonicalizationSummary(0, 0, 0, 0));
        var orchestrator = new ShwapnoIngestionOrchestrator(
            source, importer, canonicalization, new RecordingLogger());

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.IngestAsync("dairy"));

        Assert.Same(failure, thrown);
        Assert.Equal(new[] { "scrape" }, calls);
    }

    [Fact]
    public async Task IngestAsync_ShouldNotCanonicalizeWhenImportFails()
    {
        var calls = new List<string>();
        var failure = new InvalidOperationException("Import failed.");
        var source = new RecordingSource(calls, [new ShwapnoProduct { Sku = "SKU-1" }]);
        var importer = new RecordingImporter(calls, ImportResult()) { Failure = failure };
        var canonicalization = new RecordingCanonicalizationService(
            calls, new StoreProductCanonicalizationSummary(0, 0, 0, 0));
        var orchestrator = new ShwapnoIngestionOrchestrator(
            source, importer, canonicalization, new RecordingLogger());

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.IngestAsync("dairy"));

        Assert.Same(failure, thrown);
        Assert.Equal(new[] { "scrape", "import" }, calls);
    }

    [Fact]
    public async Task IngestAsync_ShouldReturnPartialSuccessAndLogWhenCanonicalizationFails()
    {
        var calls = new List<string>();
        IReadOnlyCollection<ShwapnoProduct> products =
            [new() { Sku = "SKU-1" }];
        var importResult = ImportResult();
        var failure = new InvalidOperationException("Sensitive database detail.");
        var source = new RecordingSource(calls, products);
        var importer = new RecordingImporter(calls, importResult);
        var canonicalization = new RecordingCanonicalizationService(
            calls,
            new StoreProductCanonicalizationSummary(0, 0, 0, 0))
        {
            Failure = failure
        };
        var logger = new RecordingLogger();
        var orchestrator = new ShwapnoIngestionOrchestrator(
            source, importer, canonicalization, logger);

        var result = await orchestrator.IngestAsync("dairy");

        Assert.Equal(new[] { "scrape", "import", "canonicalize" }, calls);
        Assert.Equal(
            ShwapnoIngestionStatus.ImportCompletedCanonicalizationFailed,
            result.Status);
        Assert.Equal(new ShwapnoScrapeSummary(1), result.Scrape);
        Assert.Same(importResult.Summary, result.Import);
        Assert.False(result.Canonicalization.Succeeded);
        Assert.Null(result.Canonicalization.Summary);
        Assert.Equal(
            "Products were imported, but canonicalization failed.",
            result.Canonicalization.Error);
        Assert.DoesNotContain(failure.Message, result.Canonicalization.Error);
        Assert.Equal(LogLevel.Error, logger.LastLevel);
        Assert.Same(failure, logger.LastException);
    }

    [Fact]
    public async Task IngestAsync_ShouldPropagateCancellationFromCanonicalization()
    {
        var calls = new List<string>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var source = new RecordingSource(
            calls,
            [new ShwapnoProduct { Sku = "SKU-1" }]);
        var importer = new RecordingImporter(calls, ImportResult());
        var canonicalization = new RecordingCanonicalizationService(
            calls,
            new StoreProductCanonicalizationSummary(0, 0, 0, 0))
        {
            Failure = new OperationCanceledException(cancellation.Token)
        };
        var logger = new RecordingLogger();
        var orchestrator = new ShwapnoIngestionOrchestrator(
            source, importer, canonicalization, logger);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            orchestrator.IngestAsync("dairy", cancellation.Token));

        Assert.Equal(new[] { "scrape", "import", "canonicalize" }, calls);
        Assert.Null(logger.LastException);
    }

    private static ShwapnoImportResult ImportResult() => new(
        new ShwapnoImportSummary(1, 1, 0), Guid.NewGuid(), Guid.NewGuid());

    private sealed class RecordingSource(
        List<string> calls,
        IReadOnlyCollection<ShwapnoProduct> products) : IShwapnoProductSource
    {
        public Exception? Failure { get; init; }
        public string? ReceivedCategory { get; private set; }
        public CancellationToken ReceivedToken { get; private set; }

        public Task<IReadOnlyCollection<ShwapnoProduct>> GetProductsFromShwapno(
            string category,
            CancellationToken cancellationToken = default)
        {
            calls.Add("scrape");
            ReceivedCategory = category;
            ReceivedToken = cancellationToken;
            if (Failure is not null) throw Failure;
            return Task.FromResult(products);
        }
    }

    private sealed class RecordingImporter(
        List<string> calls,
        ShwapnoImportResult result) : IShwapnoProductImporter
    {
        public Exception? Failure { get; init; }
        public string? ReceivedCategory { get; private set; }
        public IReadOnlyCollection<ShwapnoProduct>? ReceivedProducts { get; private set; }
        public CancellationToken ReceivedToken { get; private set; }

        public Task<ShwapnoImportResult> ImportAsync(
            string categorySlug,
            IReadOnlyCollection<ShwapnoProduct> sourceProducts,
            CancellationToken cancellationToken = default)
        {
            calls.Add("import");
            ReceivedCategory = categorySlug;
            ReceivedProducts = sourceProducts;
            ReceivedToken = cancellationToken;
            if (Failure is not null) throw Failure;
            return Task.FromResult(result);
        }
    }

    private sealed class RecordingCanonicalizationService(
        List<string> calls,
        StoreProductCanonicalizationSummary summary) : IStoreProductCanonicalizationService
    {
        public Exception? Failure { get; init; }
        public Guid ReceivedStoreId { get; private set; }
        public Guid ReceivedCategoryId { get; private set; }
        public CancellationToken ReceivedToken { get; private set; }

        public Task<StoreProductCanonicalizationSummary> CanonicalizePendingAsync(
            Guid storeId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            calls.Add("canonicalize");
            ReceivedStoreId = storeId;
            ReceivedCategoryId = categoryId;
            ReceivedToken = cancellationToken;
            if (Failure is not null) throw Failure;
            return Task.FromResult(summary);
        }
    }

    private sealed class RecordingLogger : ILogger<ShwapnoIngestionOrchestrator>
    {
        public LogLevel? LastLevel { get; private set; }
        public Exception? LastException { get; private set; }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
            EmptyScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            LastLevel = logLevel;
            LastException = exception;
        }

        private sealed class EmptyScope : IDisposable
        {
            public static EmptyScope Instance { get; } = new();
            public void Dispose() { }
        }
    }
}
