using CartCompareAPI.Ingestion.Shwapno;
using Microsoft.AspNetCore.Mvc;

namespace CartCompareAPI.Ingestion.Shwapno.Browser;

[ApiController]
[Route("api/v1/ingestion/shwapno")]
public class ShwapnoController(
    IShwapnoIngestionOrchestrator orchestrator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> IngestShwapnoProducts(
        [FromQuery] string category,
        CancellationToken cancellationToken)
    {
        var result = await orchestrator.IngestAsync(
            category, cancellationToken);

        return Ok(result);
    }
}