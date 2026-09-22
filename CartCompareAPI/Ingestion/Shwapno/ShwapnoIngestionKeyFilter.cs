using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace CartCompareAPI.Ingestion.Shwapno;

public class ShwapnoIngestionKeyFilter(IConfiguration configuration) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next
    )
    {
        string? configuredKey = configuration["Ingestion:ApiKey"];

        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            context.Result = KeyError(context, 503, "Ingestion unavailable");
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("X-Ingestion-Key", out StringValues suppliedHeader) ||
            suppliedHeader.Count != 1 ||
            string.IsNullOrEmpty(suppliedHeader[0]))
        {
            context.Result = KeyError(context, 401, "Unauthorized");
            return;
        }


        byte[] expected = Encoding.UTF8.GetBytes(configuredKey);
        byte[] supplied = Encoding.UTF8.GetBytes(suppliedHeader[0]!);

        if (expected.Length != supplied.Length ||
            !CryptographicOperations.FixedTimeEquals(expected, supplied))
        {
            context.Result = KeyError(context, 401, "Unauthorized");
            return;
        }

        await next();
    }
    private static IActionResult KeyError(
        ActionExecutingContext context,
        int status,
        string title
    )
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title
        };
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        return new ObjectResult(problem) { StatusCode = status };
    }

}
