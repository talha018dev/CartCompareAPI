using CartCompareAPI.Ingestion.Shwapno;
using Microsoft.AspNetCore.Mvc;

namespace CartCompareAPI.Ingestion.Shwapno.Browser;

[ApiController]
[Route("api/v1/ingestion/shwapno")]
public class ShwapnoController(
    IShwapnoIngestionOrchestrator orchestrator) : ControllerBase
{
    [HttpPost]
    [ServiceFilter(typeof(ShwapnoIngestionKeyFilter), Order = 0)]
    [ServiceFilter(typeof(ShwapnoIngestionConcurrencyFilter), Order = 1)]
    public async Task<IActionResult> IngestShwapnoProducts(
        [FromQuery] string category,
        CancellationToken cancellationToken)
    {
        var result = await orchestrator.IngestAsync(
            category, cancellationToken);

        return Ok(result);
    }

}
