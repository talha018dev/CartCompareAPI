using System.Security.Cryptography;
using System.Text;
using CartCompareAPI.Ingestion.Shwapno;
using Microsoft.AspNetCore.Mvc;

namespace CartCompareAPI.Ingestion.Shwapno.Browser;

[ApiController]
[Route("api/v1/ingestion/shwapno")]
public class ShwapnoController(
    IShwapnoIngestionOrchestrator orchestrator,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> IngestShwapnoProducts(
        [FromQuery] string category,
        CancellationToken cancellationToken)
    {
        string? configuredKey = configuration["Ingestion:ApiKey"];

        if (string.IsNullOrWhiteSpace(configuredKey))
            return KeyError(503, "Ingestion unavailable");

        if (!Request.Headers.TryGetValue("X-Ingestion-Key", out var suppliedHeader) ||
            suppliedHeader.Count != 1 ||
            string.IsNullOrEmpty(suppliedHeader[0]))
        {
            return KeyError(401, "Unauthorized");
        }

        byte[] expected = Encoding.UTF8.GetBytes(configuredKey);
        byte[] supplied = Encoding.UTF8.GetBytes(suppliedHeader[0]!);

        if (expected.Length != supplied.Length ||
            !CryptographicOperations.FixedTimeEquals(expected, supplied))
        {
            return KeyError(401, "Unauthorized");
        }

        var result = await orchestrator.IngestAsync(
            category, cancellationToken);

        return Ok(result);
    }

    private IActionResult KeyError(int status, string title)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title
        };
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;

        return StatusCode(status, problem);
    }
}