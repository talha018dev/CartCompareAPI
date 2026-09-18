using CartCompareAPI.Canonicalization.StoreProducts;
using CartCompareAPI.Ingestion.Shwapno;
using CartCompareAPI.Ingestion.Shwapno.Browser;
using CartCompareAPI.Ingestion.Shwapno.Import;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace CartCompareAPI.Tests.Ingestion.Shwapno;

public sealed class ShwapnoControllerTests
{
    private const string ApiKey = "test-ingestion-key";

    [Fact]
    public async Task MissingServerKey_ShouldReturn503WithoutCallingOrchestrator()
    {
        var orchestrator = SuccessfulOrchestrator();
        var controller = CreateController(orchestrator, configuredKey: null, suppliedKey: ApiKey);

        var action = await controller.IngestShwapnoProducts("dairy", CancellationToken.None);

        AssertProblem(action, 503, "Ingestion unavailable");
        Assert.Equal(0, orchestrator.CallCount);
    }

    [Fact]
    public async Task MissingHeader_ShouldReturn401WithoutCallingOrchestrator()
    {
        var orchestrator = SuccessfulOrchestrator();
        var controller = CreateController(orchestrator, configuredKey: ApiKey);

        var action = await controller.IngestShwapnoProducts("dairy", CancellationToken.None);

        AssertProblem(action, 401, "Unauthorized");
        Assert.Equal(0, orchestrator.CallCount);
    }

    [Theory]
    [InlineData("wrong-ingestion-key")]
    [InlineData("wrong")]
    public async Task WrongHeader_ShouldReturn401WithoutCallingOrchestrator(string suppliedKey)
    {
        var orchestrator = SuccessfulOrchestrator();
        var controller = CreateController(orchestrator, configuredKey: ApiKey, suppliedKey: suppliedKey);

        var action = await controller.IngestShwapnoProducts("dairy", CancellationToken.None);

        AssertProblem(action, 401, "Unauthorized");
        Assert.Equal(0, orchestrator.CallCount);
    }

    [Fact]
    public async Task MultipleHeaderValues_ShouldReturn401WithoutCallingOrchestrator()
    {
        var orchestrator = SuccessfulOrchestrator();
        var controller = CreateController(orchestrator, configuredKey: ApiKey);
        controller.Request.Headers["X-Ingestion-Key"] = new StringValues([ApiKey, "other-key"]);

        var action = await controller.IngestShwapnoProducts("dairy", CancellationToken.None);

        AssertProblem(action, 401, "Unauthorized");
        Assert.Equal(0, orchestrator.CallCount);
    }

    [Fact]
    public async Task CorrectHeader_ShouldReturnSuccessfulResult()
    {
        var expected = SampleResult();
        var orchestrator = new FakeOrchestrator((_, _) => Task.FromResult(expected));
        var controller = CreateController(orchestrator, configuredKey: ApiKey, suppliedKey: ApiKey);
        using var cancellation = new CancellationTokenSource();

        var action = await controller.IngestShwapnoProducts("dairy", cancellation.Token);

        Assert.Same(expected, Assert.IsType<OkObjectResult>(action).Value);
        Assert.Equal(1, orchestrator.CallCount);
        Assert.Equal("dairy", orchestrator.LastCategory);
        Assert.Equal(cancellation.Token, orchestrator.LastToken);
    }

    [Fact]
    public async Task IngestionFailure_ShouldReachExceptionMiddleware()
    {
        var failure = new InvalidOperationException("Ingestion failed.");
        var orchestrator = new FakeOrchestrator((_, _) =>
            Task.FromException<ShwapnoIngestionResult>(failure));
        var controller = CreateController(orchestrator, configuredKey: ApiKey, suppliedKey: ApiKey);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            controller.IngestShwapnoProducts("dairy", CancellationToken.None));

        Assert.Same(failure, actual);
        Assert.Equal(1, orchestrator.CallCount);
    }

    [Fact]
    public async Task RequestCancellation_ShouldPropagate()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var orchestrator = new FakeOrchestrator((_, token) =>
            Task.FromCanceled<ShwapnoIngestionResult>(token));
        var controller = CreateController(orchestrator, configuredKey: ApiKey, suppliedKey: ApiKey);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            controller.IngestShwapnoProducts("dairy", cancellation.Token));

        Assert.Equal(1, orchestrator.CallCount);
    }

    private static ShwapnoController CreateController(
        IShwapnoIngestionOrchestrator orchestrator,
        string? configuredKey,
        string? suppliedKey = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ingestion:ApiKey"] = configuredKey
            })
            .Build();
        var context = new DefaultHttpContext { TraceIdentifier = "test-trace-123" };
        if (suppliedKey is not null)
            context.Request.Headers["X-Ingestion-Key"] = suppliedKey;

        return new ShwapnoController(orchestrator, configuration)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static void AssertProblem(IActionResult action, int status, string title)
    {
        var response = Assert.IsType<ObjectResult>(action);
        Assert.Equal(status, response.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(response.Value);
        Assert.Equal(status, problem.Status);
        Assert.Equal(title, problem.Title);
        Assert.Equal("test-trace-123", problem.Extensions["traceId"]);
    }

    private static FakeOrchestrator SuccessfulOrchestrator() =>
        new((_, _) => Task.FromResult(SampleResult()));

    private static ShwapnoIngestionResult SampleResult() => new(
        new ShwapnoScrapeSummary(2),
        new ShwapnoImportSummary(2, 1, 1),
        new StoreProductCanonicalizationSummary(1, 1, 0, 0));

    private sealed class FakeOrchestrator(
        Func<string, CancellationToken, Task<ShwapnoIngestionResult>> ingest)
        : IShwapnoIngestionOrchestrator
    {
        public int CallCount { get; private set; }
        public string? LastCategory { get; private set; }
        public CancellationToken LastToken { get; private set; }

        public Task<ShwapnoIngestionResult> IngestAsync(
            string categorySlug,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastCategory = categorySlug;
            LastToken = cancellationToken;
            return ingest(categorySlug, cancellationToken);
        }
    }
}
