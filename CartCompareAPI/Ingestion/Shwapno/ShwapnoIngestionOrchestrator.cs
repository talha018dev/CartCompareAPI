using CartCompareAPI.Canonicalization.StoreProducts;
using CartCompareAPI.Ingestion.Shwapno.Browser;
using CartCompareAPI.Ingestion.Shwapno.Entities;
using CartCompareAPI.Ingestion.Shwapno.Import;

namespace CartCompareAPI.Ingestion.Shwapno;

public sealed class ShwapnoIngestionOrchestrator(
    IShwapnoProductSource productSource,
    IShwapnoProductImporter importer,
    IStoreProductCanonicalizationService canonicalizationService,
    ILogger<ShwapnoIngestionOrchestrator> logger) : IShwapnoIngestionOrchestrator
{
    public async Task<ShwapnoIngestionResult> IngestAsync(
        string categorySlug,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ShwapnoProduct> products = await productSource.GetProductsFromShwapno(
        categorySlug, cancellationToken);

        ShwapnoImportResult importResult = await importer.ImportAsync(
            categorySlug, products, cancellationToken);

        var scrapeSummary = new ShwapnoScrapeSummary(products.Count);

        try
        {
            StoreProductCanonicalizationSummary summary = await canonicalizationService.CanonicalizePendingAsync(
            importResult.StoreId,
            importResult.CategoryId,
            cancellationToken);

            return new ShwapnoIngestionResult(
            ShwapnoIngestionStatus.Completed,
            scrapeSummary,
            importResult.Summary,
            new ShwapnoCanonicalizationResult(
                Succeeded: true,
                Summary: summary,
                Error: null));
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                """
                Shwapno canonicalization failed after import completed.
                StoreId {StoreId}, CategoryId {CategoryId}
                """,
                importResult.StoreId,
                importResult.CategoryId);

            return new ShwapnoIngestionResult(
                ShwapnoIngestionStatus.ImportCompletedCanonicalizationFailed,
                scrapeSummary,
                importResult.Summary,
                new ShwapnoCanonicalizationResult(
                    Succeeded: false,
                    Summary: null,
                    Error:
                        "Products were imported, but canonicalization failed."));
        }




    }
}
