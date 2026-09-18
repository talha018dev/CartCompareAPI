using CartCompareAPI.Canonicalization.StoreProducts;
using CartCompareAPI.Ingestion.Shwapno;
using CartCompareAPI.Ingestion.Shwapno.Browser;
using CartCompareAPI.Ingestion.Shwapno.Import;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CartCompareAPI.Tests.Ingestion.Shwapno;

public sealed class ShwapnoControllerTests
{
    [Fact]
    public async Task IngestShwapnoProducts_ShouldReturn400ForUnsupportedCategory()
    {
        var failure = new UnsupportedShwapnoCategoryException("Category was not found.");
        var controller = CreateController(FailingOrchestrator(failure), new RecordingLogger());

        var action = await controller.IngestShwapnoProducts("unknown", CancellationToken.None);

        var problem = AssertProblem(action, 400);
        Assert.Equal("Unsupported category", problem.Title);
        Assert.Equal(failure.Message, problem.Detail);
    }

    [Fact]
    public async Task IngestShwapnoProducts_ShouldReturn422ForInvalidSourceData()
    {
        var failure = new InvalidShwapnoSourceDataException("A product is missing its SKU.");
        var controller = CreateController(FailingOrchestrator(failure), new RecordingLogger());

        var action = await controller.IngestShwapnoProducts("dairy", CancellationToken.None);

        var problem = AssertProblem(action, 422);
        Assert.Equal("Invalid Shwapno product data", problem.Title);
        Assert.Equal(failure.Message, problem.Detail);
    }

    [Fact]
    public async Task IngestShwapnoProducts_ShouldLogUnexpectedFailureAndReturnSafe500()
    {
        var failure = new InvalidOperationException("Sensitive internal database detail.");
        var logger = new RecordingLogger();
        var controller = CreateController(FailingOrchestrator(failure), logger);

        var action = await controller.IngestShwapnoProducts("dairy", CancellationToken.None);

        var problem = AssertProblem(action, 500);
        Assert.Equal("Ingestion failed", problem.Title);
        Assert.DoesNotContain(failure.Message, problem.Detail);
        Assert.Equal(LogLevel.Error, logger.LastLevel);
        Assert.Same(failure, logger.LastException);
    }

    [Fact]
    public async Task IngestShwapnoProducts_ShouldLetRequestCancellationPropagate()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var logger = new RecordingLogger();
        var orchestrator = new FakeOrchestrator((_, token) =>
            Task.FromCanceled<ShwapnoIngestionResult>(token));
        var controller = CreateController(orchestrator, logger);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            controller.IngestShwapnoProducts("dairy", cancellation.Token));

        Assert.Null(logger.LastException);
    }

    [Fact]
    public async Task IngestShwapnoProducts_ShouldReturnSuccessfulResult()
    {
        var expected = new ShwapnoIngestionResult(
            new ShwapnoScrapeSummary(2),
            new ShwapnoImportSummary(2, 1, 1),
            new StoreProductCanonicalizationSummary(1, 1, 0, 0));
        var orchestrator = new FakeOrchestrator((_, _) => Task.FromResult(expected));
        var controller = CreateController(orchestrator, new RecordingLogger());

        var action = await controller.IngestShwapnoProducts("dairy", CancellationToken.None);

        Assert.Same(expected, Assert.IsType<OkObjectResult>(action).Value);
    }

    private static ShwapnoController CreateController(
        IShwapnoIngestionOrchestrator orchestrator,
        RecordingLogger logger) => new(orchestrator, logger)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { TraceIdentifier = "test-trace-123" }
        }
    };

    private static IShwapnoIngestionOrchestrator FailingOrchestrator(Exception failure) =>
        new FakeOrchestrator((_, _) =>
            Task.FromException<ShwapnoIngestionResult>(failure));

    private static ProblemDetails AssertProblem(IActionResult action, int expectedStatus)
    {
        var response = Assert.IsType<ObjectResult>(action);
        Assert.Equal(expectedStatus, response.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(response.Value);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.Equal("test-trace-123", problem.Extensions["traceId"]);
        return problem;
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

    private sealed class RecordingLogger : ILogger<ShwapnoController>
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
