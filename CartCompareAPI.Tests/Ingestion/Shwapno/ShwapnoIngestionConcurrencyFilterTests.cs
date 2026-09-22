using CartCompareAPI.Ingestion.Shwapno;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace CartCompareAPI.Tests.Ingestion.Shwapno;

public sealed class ShwapnoIngestionConcurrencyFilterTests
{
    [Fact]
    public async Task ConcurrentRequest_ShouldReturn409WithoutRunningNextAction()
    {
        using var guard = new ShwapnoIngestionConcurrencyGuard();
        var filter = new ShwapnoIngestionConcurrencyFilter(guard);
        var firstStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int actionCallCount = 0;

        ActionExecutingContext firstContext = CreateContext("first-trace");
        Task firstRequest = filter.OnActionExecutionAsync(firstContext, async () =>
        {
            Interlocked.Increment(ref actionCallCount);
            firstStarted.SetResult();
            await releaseFirst.Task;
            return CreateExecutedContext(firstContext);
        });

        await firstStarted.Task;

        ActionExecutingContext secondContext = CreateContext("second-trace");
        await filter.OnActionExecutionAsync(secondContext, () =>
        {
            Interlocked.Increment(ref actionCallCount);
            return Task.FromResult(CreateExecutedContext(secondContext));
        });

        var response = Assert.IsType<ObjectResult>(secondContext.Result);
        Assert.Equal(StatusCodes.Status409Conflict, response.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(response.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("Ingestion already in progress", problem.Title);
        Assert.Equal("second-trace", problem.Extensions["traceId"]);
        Assert.Equal(1, Volatile.Read(ref actionCallCount));

        releaseFirst.SetResult();
        await firstRequest;
    }

    [Fact]
    public async Task CompletedRequest_ShouldReleaseGuardForLaterRequest()
    {
        using var guard = new ShwapnoIngestionConcurrencyGuard();
        var filter = new ShwapnoIngestionConcurrencyFilter(guard);
        int actionCallCount = 0;

        await ExecuteSuccessfulRequest(filter, "first-trace", () => actionCallCount++);
        await ExecuteSuccessfulRequest(filter, "second-trace", () => actionCallCount++);

        Assert.Equal(2, actionCallCount);
    }

    [Fact]
    public async Task FailedRequest_ShouldRethrowAndReleaseGuard()
    {
        using var guard = new ShwapnoIngestionConcurrencyGuard();
        var filter = new ShwapnoIngestionConcurrencyFilter(guard);
        var failure = new InvalidOperationException("Ingestion failed.");
        ActionExecutingContext failedContext = CreateContext("failed-trace");

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filter.OnActionExecutionAsync(
                failedContext,
                () => Task.FromException<ActionExecutedContext>(failure)));

        Assert.Same(failure, actual);

        int laterActionCallCount = 0;
        await ExecuteSuccessfulRequest(
            filter,
            "later-trace",
            () => laterActionCallCount++);
        Assert.Equal(1, laterActionCallCount);
    }

    [Fact]
    public async Task CancelledRequest_ShouldRethrowAndReleaseGuard()
    {
        using var guard = new ShwapnoIngestionConcurrencyGuard();
        var filter = new ShwapnoIngestionConcurrencyFilter(guard);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        ActionExecutingContext cancelledContext = CreateContext("cancelled-trace");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            filter.OnActionExecutionAsync(
                cancelledContext,
                () => Task.FromCanceled<ActionExecutedContext>(cancellation.Token)));

        int laterActionCallCount = 0;
        await ExecuteSuccessfulRequest(
            filter,
            "later-trace",
            () => laterActionCallCount++);
        Assert.Equal(1, laterActionCallCount);
    }

    private static Task ExecuteSuccessfulRequest(
        ShwapnoIngestionConcurrencyFilter filter,
        string traceId,
        Action action)
    {
        ActionExecutingContext context = CreateContext(traceId);
        return filter.OnActionExecutionAsync(context, () =>
        {
            action();
            return Task.FromResult(CreateExecutedContext(context));
        });
    }

    private static ActionExecutingContext CreateContext(string traceId)
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = traceId
        };
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller: new object());
    }

    private static ActionExecutedContext CreateExecutedContext(
        ActionExecutingContext context) => new(
            context,
            [],
            controller: new object());
}
