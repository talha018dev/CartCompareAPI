using CartCompareAPI.Ingestion.Shwapno.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CartCompareAPI.Ingestion.Shwapno.Browser;

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
    public async Task<IActionResult> IngestShwapnoProducts(
        [FromQuery] string category,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ShwapnoProduct> products = await _browser.GetProductsFromShwapno(category, cancellationToken);

        return Ok(new { Category = category, ProductsCollected = products.Count });
    }
}
