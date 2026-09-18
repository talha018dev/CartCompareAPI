using CartCompareAPI.Canonicalization.StoreProducts;
using CartCompareAPI.Ingestion.Shwapno;
using CartCompareAPI.Ingestion.Shwapno.Browser;
using CartCompareAPI.Ingestion.Shwapno.Import;
using Microsoft.AspNetCore.Mvc;

namespace CartCompareAPI.Tests.Ingestion.Shwapno;

public sealed class ShwapnoControllerTests
{
    [Fact]
    public async Task IngestShwapnoProducts_ShouldReturnSuccessfulResult()
    {
        var expected = new ShwapnoIngestionResult(
            new ShwapnoScrapeSummary(2),
            new ShwapnoImportSummary(2, 1, 1),
            new StoreProductCanonicalizationSummary(1, 1, 0, 0));
        var orchestrator = new FakeOrchestrator((_, _) => Task.FromResult(expected));
        var controller = new ShwapnoController(orchestrator);

        var action = await controller.IngestShwapnoProducts("dairy", CancellationToken.None);

        Assert.Same(expected, Assert.IsType<OkObjectResult>(action).Value);
    }

    [Fact]
    public async Task IngestShwapnoProducts_ShouldLetFailureReachExceptionMiddleware()
    {
        var failure = new InvalidOperationException("Ingestion failed.");
        var controller = new ShwapnoController(new FakeOrchestrator((_, _) =>
            Task.FromException<ShwapnoIngestionResult>(failure)));

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            controller.IngestShwapnoProducts("dairy", CancellationToken.None));

        Assert.Same(failure, actual);
    }

    [Fact]
    public async Task IngestShwapnoProducts_ShouldLetRequestCancellationPropagate()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var controller = new ShwapnoController(new FakeOrchestrator((_, token) =>
            Task.FromCanceled<ShwapnoIngestionResult>(token)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            controller.IngestShwapnoProducts("dairy", cancellation.Token));
    }

    private sealed class FakeOrchestrator(
        Func<string, CancellationToken, Task<ShwapnoIngestionResult>> ingest)
        : IShwapnoIngestionOrchestrator
    {
        public Task<ShwapnoIngestionResult> IngestAsync(
            string categorySlug,
            CancellationToken cancellationToken = default) =>
            ingest(categorySlug, cancellationToken);
    }
}
