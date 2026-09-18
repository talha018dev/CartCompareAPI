using System;
using Microsoft.AspNetCore.Diagnostics;

namespace CartCompareAPI.Ingestion.Shwapno;

public class ShwapnoIngestionExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {

        (int status, string title) = exception switch
        {
            UnsupportedShwapnoCategoryException => (400, "Unsupported category"),
            InvalidShwapnoSourceDataException => (422, "Invalid Shwapno product data"),
            _ => (0, "")
        };

        if (status == 0)
        {
            return false;
        }

        await Results.Problem(
            statusCode: status,
            title: title,
            detail: exception.Message,
            extensions: new Dictionary<string, object?>
            {
                ["traceId"] = httpContext.TraceIdentifier
            }).ExecuteAsync(httpContext);

        return true;
    }
}
