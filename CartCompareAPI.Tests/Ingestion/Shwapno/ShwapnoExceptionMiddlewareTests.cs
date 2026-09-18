using System.Diagnostics;
using System.Text.Json;
using CartCompareAPI.Infrastructure;
using CartCompareAPI.Ingestion.Shwapno;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CartCompareAPI.Tests.Ingestion.Shwapno;

public sealed class ShwapnoExceptionMiddlewareTests
{
    [Theory]
    [InlineData(true, 400, "Unsupported category")]
    [InlineData(false, 422, "Invalid Shwapno product data")]
    public async Task KnownFailure_ShouldReturnProblemWithTraceId(
        bool unsupportedCategory,
        int expectedStatus,
        string expectedTitle)
    {
        Exception failure = unsupportedCategory
            ? new UnsupportedShwapnoCategoryException("Unknown category.")
            : new InvalidShwapnoSourceDataException("Product has no SKU.");
        var logger = new RecordingLogger();

        using var services = CreateServices(logger);
        using var body = new MemoryStream();
        var context = CreateContext(services, body);
        var pipeline = CreatePipeline(services, failure);

        await pipeline(context);

        using var response = ReadResponse(body);
        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
        Assert.Equal(expectedStatus, response.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(expectedTitle, response.RootElement.GetProperty("title").GetString());
        Assert.Equal(failure.Message, response.RootElement.GetProperty("detail").GetString());
        Assert.Equal("test-trace-123", response.RootElement.GetProperty("traceId").GetString());
        Assert.Null(logger.LastException);
    }

    [Fact]
    public async Task UnexpectedFailure_ShouldLogExceptionAndReturnSafe500()
    {
        var failure = new InvalidOperationException("Sensitive internal database detail.");
        var logger = new RecordingLogger();

        using var services = CreateServices(logger);
        using var body = new MemoryStream();
        var context = CreateContext(services, body);
        var pipeline = CreatePipeline(services, failure);

        await pipeline(context);

        using var response = ReadResponse(body);
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal(500, response.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Ingestion failed", response.RootElement.GetProperty("title").GetString());
        Assert.Equal("An unexpected error occurred.",
            response.RootElement.GetProperty("detail").GetString());
        Assert.DoesNotContain(
            failure.Message,
            System.Text.Encoding.UTF8.GetString(body.ToArray()));
        Assert.Equal("test-trace-123", response.RootElement.GetProperty("traceId").GetString());
        Assert.Equal(LogLevel.Error, logger.LastLevel);
        Assert.Same(failure, logger.LastException);
    }

    private static ServiceProvider CreateServices(RecordingLogger logger)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMetrics();
        services.AddSingleton(new DiagnosticListener("ShwapnoExceptionMiddlewareTests"));
        services.AddSingleton<DiagnosticSource>(provider =>
            provider.GetRequiredService<DiagnosticListener>());
        services.AddProblemDetails();
        services.AddExceptionHandler<ShwapnoIngestionExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddSingleton<ILogger<GlobalExceptionHandler>>(logger);
        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateContext(IServiceProvider services, Stream body)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = services,
            TraceIdentifier = "test-trace-123"
        };
        context.Response.Body = body;
        return context;
    }

    private static RequestDelegate CreatePipeline(IServiceProvider services, Exception failure)
    {
        var app = new ApplicationBuilder(services);
        app.UseExceptionHandler();
        app.Run(_ => Task.FromException(failure));
        return app.Build();
    }

    private static JsonDocument ReadResponse(Stream body)
    {
        body.Position = 0;
        return JsonDocument.Parse(body);
    }

    private sealed class RecordingLogger : ILogger<GlobalExceptionHandler>
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
