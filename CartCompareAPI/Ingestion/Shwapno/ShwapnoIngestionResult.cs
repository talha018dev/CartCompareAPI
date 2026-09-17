using CartCompareAPI.Canonicalization.StoreProducts;
using CartCompareAPI.Ingestion.Shwapno.Import;

namespace CartCompareAPI.Ingestion.Shwapno;

public sealed record ShwapnoScrapeSummary(int ProductsCollected);

public sealed record ShwapnoIngestionResult(
    ShwapnoScrapeSummary Scrape,
    ShwapnoImportSummary Import,
    StoreProductCanonicalizationSummary Canonicalization);
