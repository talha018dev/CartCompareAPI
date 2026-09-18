using System;

namespace CartCompareAPI.Ingestion.Shwapno;

public interface IShwapnoIngestionOrchestrator
{
    Task<ShwapnoIngestionResult> IngestAsync(
        string categorySlug,
        CancellationToken cancellationToken = default);
}
