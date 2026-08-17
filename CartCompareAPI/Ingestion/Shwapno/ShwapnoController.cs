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

    [HttpGet("test-browser")]
    public async Task<IActionResult> TestBrowser()
    {
        await _browser.TestAsync();

        return Ok("Playwright worked");
    }
}