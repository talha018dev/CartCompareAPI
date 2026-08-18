using Microsoft.AspNetCore.Mvc;

namespace CartCompareApi.Ingestion.Shwapno.Browser;

[ApiController]
[Route("api/v1/ingestion/shwapno")]
public class ShwapnoController : ControllerBase
{
    private readonly ShwapnoBrowserClient _browser;

    public ShwapnoController(ShwapnoBrowserClient browser)
    {
        _browser = browser;
    }

    [HttpGet]
    public async Task<IActionResult> IngestShwapnoProducts([FromQuery] string category)
    {
        await _browser.GetProductsFromShwapno(category);

        return Ok($"Shwapno {category} products ingested successfully.");
    }
}
