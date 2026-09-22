using System.Text.Json.Serialization;
using CartCompareAPI.Canonicalization.StoreProducts;
using CartCompareAPI.Ingestion.Shwapno.Import;

namespace CartCompareAPI.Ingestion.Shwapno;

public sealed record ShwapnoScrapeSummary(int ProductsCollected);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShwapnoIngestionStatus
{
    Completed,
    ImportCompletedCanonicalizationFailed,
}

public sealed record ShwapnoCanonicalizationResult(
    bool Succeeded,
    StoreProductCanonicalizationSummary? Summary,
    string? Error);

public sealed record ShwapnoIngestionResult(
    ShwapnoIngestionStatus Status,
    ShwapnoScrapeSummary Scrape,
    ShwapnoImportSummary Import,
    ShwapnoCanonicalizationResult Canonicalization);
