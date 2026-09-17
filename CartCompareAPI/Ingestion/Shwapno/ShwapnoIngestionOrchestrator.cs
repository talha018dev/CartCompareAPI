using CartCompareAPI.Canonicalization.StoreProducts;
using CartCompareAPI.Ingestion.Shwapno.Browser;
using CartCompareAPI.Ingestion.Shwapno.Import;

namespace CartCompareAPI.Ingestion.Shwapno;

public sealed class ShwapnoIngestionOrchestrator(
    IShwapnoProductSource productSource,
    IShwapnoProductImporter importer,
    IStoreProductCanonicalizationService canonicalizationService)
{
    public async Task<ShwapnoIngestionResult> IngestAsync(
        string categorySlug,
        CancellationToken cancellationToken = default)
    {
        var products = await productSource.GetProductsFromShwapno(
        categorySlug, cancellationToken);

        var importResult = await importer.ImportAsync(
            categorySlug, products, cancellationToken);

        var canonicalization = await canonicalizationService.CanonicalizePendingAsync(
            importResult.StoreId,
            importResult.CategoryId,
            cancellationToken);

        return new ShwapnoIngestionResult(
            new ShwapnoScrapeSummary(products.Count),
            importResult.Summary,
            canonicalization);
    }
}
