using System.Text.Json;
using CartCompareAPI.Canonicalization.StoreProducts;
using CartCompareAPI.Ingestion.Shwapno;
using CartCompareAPI.Ingestion.Shwapno.Browser;
using CartCompareAPI.Ingestion.Shwapno.Import;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace CartCompareAPI.Tests.Ingestion.Shwapno;

public sealed class ShwapnoIngestionKeyFilterTests
{
    private const string ApiKey = "test-ingestion-key";

    [Fact]
    public async Task MissingServerKey_ShouldReturn503WithoutStartingIngestion()
    {
        using var response = await SendRequest(configuredKey: null, suppliedKey: ApiKey);

        AssertProblem(response, 503, "Ingestion unavailable");
        Assert.Equal(0, response.Orchestrator.CallCount);
    }

    [Fact]
    public async Task MissingHeader_ShouldReturn401WithoutStartingIngestion()
    {
        using var response = await SendRequest(configuredKey: ApiKey);

        AssertProblem(response, 401, "Unauthorized");
        Assert.Equal(0, response.Orchestrator.CallCount);
    }

    [Theory]
    [InlineData("wrong-ingestion-key")]
    [InlineData("wrong")]
    public async Task WrongHeader_ShouldReturn401WithoutStartingIngestion(string suppliedKey)
    {
        using var response = await SendRequest(configuredKey: ApiKey, suppliedKey: suppliedKey);

        AssertProblem(response, 401, "Unauthorized");
        Assert.Equal(0, response.Orchestrator.CallCount);
    }

    [Fact]
    public async Task MultipleHeaderValues_ShouldReturn401WithoutStartingIngestion()
    {
        using var response = await SendRequest(
            configuredKey: ApiKey,
            suppliedHeaders: new StringValues([ApiKey, "other-key"]));

        AssertProblem(response, 401, "Unauthorized");
        Assert.Equal(0, response.Orchestrator.CallCount);
    }

    [Fact]
    public async Task CorrectHeader_ShouldRunIngestion()
    {
        using var response = await SendRequest(configuredKey: ApiKey, suppliedKey: ApiKey);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal(1, response.Orchestrator.CallCount);
        Assert.Equal("dairy", response.Orchestrator.LastCategory);
    }

    private static async Task<TestResponse> SendRequest(
        string? configuredKey,
        string? suppliedKey = null,
        StringValues suppliedHeaders = default)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ingestion:ApiKey"] = configuredKey
            })
            .Build();
        var orchestrator = new RecordingOrchestrator();
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers().AddApplicationPart(typeof(ShwapnoController).Assembly);
        builder.Services.AddSingleton<IConfiguration>(configuration);
        builder.Services.AddSingleton<IShwapnoIngestionOrchestrator>(orchestrator);
        builder.Services.AddScoped<ShwapnoIngestionKeyFilter>();
        builder.Services.AddSingleton<ShwapnoIngestionConcurrencyGuard>();
        builder.Services.AddScoped<ShwapnoIngestionConcurrencyFilter>();
        await using var host = builder.Build();
        using var scope = host.Services.CreateScope();

        var app = new ApplicationBuilder(host.Services);
        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
        RequestDelegate pipeline = app.Build();

        var context = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            TraceIdentifier = "test-trace-123"
        };
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/ingestion/shwapno";
        context.Request.QueryString = new QueryString("?category=dairy");
        if (suppliedKey is not null)
            context.Request.Headers["X-Ingestion-Key"] = suppliedKey;
        else if (suppliedHeaders.Count > 0)
            context.Request.Headers["X-Ingestion-Key"] = suppliedHeaders;

        using var body = new MemoryStream();
        context.Response.Body = body;
        await pipeline(context);

        body.Position = 0;
        JsonDocument? json = body.Length > 0 ? JsonDocument.Parse(body) : null;
        return new TestResponse(context.Response.StatusCode, json, orchestrator);
    }

    private static void AssertProblem(TestResponse response, int status, string title)
    {
        Assert.Equal(status, response.StatusCode);
        JsonElement problem = Assert.IsType<JsonDocument>(response.Json).RootElement;
        Assert.Equal(status, problem.GetProperty("status").GetInt32());
        Assert.Equal(title, problem.GetProperty("title").GetString());
        Assert.Equal("test-trace-123", problem.GetProperty("traceId").GetString());
    }

    private sealed class TestResponse(
        int statusCode,
        JsonDocument? json,
        RecordingOrchestrator orchestrator) : IDisposable
    {
        public int StatusCode { get; } = statusCode;
        public JsonDocument? Json { get; } = json;
        public RecordingOrchestrator Orchestrator { get; } = orchestrator;

        public void Dispose()
        {
            Json?.Dispose();
        }
    }

    private sealed class RecordingOrchestrator : IShwapnoIngestionOrchestrator
    {
        public int CallCount { get; private set; }
        public string? LastCategory { get; private set; }

        public Task<ShwapnoIngestionResult> IngestAsync(
            string categorySlug,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastCategory = categorySlug;
            return Task.FromResult(new ShwapnoIngestionResult(
                ShwapnoIngestionStatus.Completed,
                new ShwapnoScrapeSummary(2),
                new ShwapnoImportSummary(2, 1, 1),
                new ShwapnoCanonicalizationResult(
                    true,
                    new StoreProductCanonicalizationSummary(1, 1, 0, 0),
                    null)));
        }
    }
}
