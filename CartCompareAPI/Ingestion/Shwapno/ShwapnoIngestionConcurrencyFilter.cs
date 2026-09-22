using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CartCompareAPI.Ingestion.Shwapno;

public class ShwapnoIngestionConcurrencyFilter(ShwapnoIngestionConcurrencyGuard guard) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        bool acquired = await guard.TryAcquireAsync(context.HttpContext.RequestAborted);

        if (!acquired)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Ingestion already in progress",
                Detail = "Another ingestion process is currently running. Please try again later."
            };

            problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            context.Result = new ObjectResult(problem)
            {
                StatusCode = StatusCodes.Status409Conflict
            };

            return;
        }

        try
        {
            await next();
        }
        finally
        {
            guard.Release();
        }
    }
}
