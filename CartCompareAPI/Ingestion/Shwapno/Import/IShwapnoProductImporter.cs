using CartCompareAPI.Ingestion.Shwapno.Entities;

namespace CartCompareAPI.Ingestion.Shwapno.Import;

public interface IShwapnoProductImporter
{
    Task<ShwapnoImportResult> ImportAsync(
        string categorySlug,
        IReadOnlyCollection<ShwapnoProduct> sourceProducts,
        CancellationToken cancellationToken = default);
}