using CartCompareAPI.Canonicalization.StoreProducts;
using CartCompareAPI.Ingestion.Shwapno.Browser;
using CartCompareAPI.Ingestion.Shwapno.Import;

namespace CartCompareAPI.Ingestion.Shwapno;

public sealed class ShwapnoIngestionOrchestrator(
    IShwapnoProductSource productSource,
    IShwapnoProductImporter importer,
    IStoreProductCanonicalizationService canonicalizationService)
{
    public Task<ShwapnoIngestionResult> IngestAsync(
        string categorySlug,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
