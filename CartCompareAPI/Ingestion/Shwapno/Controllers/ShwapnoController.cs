using Microsoft.AspNetCore.Mvc;

namespace CartCompareApi.Ingestion.Shwapno.Browser;

[ApiController]
[Route("api/ingestion/shwapno")]
public class ShwapnoController : ControllerBase
{
    private readonly ShwapnoBrowserClient _browser;

    public ShwapnoController(ShwapnoBrowserClient browser)
    {
        _browser = browser;
    }

    [HttpGet("dairy")]
    public async Task<IActionResult> IngestShwapnoProducts()
    {
        await _browser.GetProductsFromShwapno();

        return Ok("Playwright worked");
    }
}