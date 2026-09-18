using CartCompareAPI.Ingestion.Shwapno;
using Microsoft.AspNetCore.Mvc;

namespace CartCompareAPI.Ingestion.Shwapno.Browser;

[ApiController]
[Route("api/v1/ingestion/shwapno")]
public class ShwapnoController(
    IShwapnoIngestionOrchestrator orchestrator,
    ILogger<ShwapnoController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> IngestShwapnoProducts(
        [FromQuery] string category,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await orchestrator.IngestAsync(
                category, cancellationToken);

            return Ok(result);
        }
        catch (UnsupportedShwapnoCategoryException ex)
        {
            return Error(400, "Unsupported category", ex.Message);
        }
        catch (InvalidShwapnoSourceDataException ex)
        {
            return Error(422, "Invalid Shwapno product data", ex.Message);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Shwapno ingestion failed for category {Category}. TraceId {TraceId}",
                category,
                HttpContext.TraceIdentifier);

            return Error(
                500,
                "Ingestion failed",
                "An unexpected error occurred.");
        }
    }

    private IActionResult Error(int status, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };

        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(status, problem);
    }
}