using System;
using Microsoft.AspNetCore.Diagnostics;

namespace CartCompareAPI.Infrastructure;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        logger.LogError(
            exception,
            "Unhandled request error. TraceId {TraceId}",
            httpContext.TraceIdentifier);

        await Results.Problem(
            statusCode: 500,
            title: "Ingestion failed",
            detail: "An unexpected error occurred.",
            extensions: new Dictionary<string, object?>
            {
                ["traceId"] = httpContext.TraceIdentifier
            }).ExecuteAsync(httpContext);

        return true;
    }
}
