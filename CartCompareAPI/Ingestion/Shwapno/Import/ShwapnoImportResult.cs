namespace CartCompareAPI.Ingestion.Shwapno.Import;

public sealed record ShwapnoImportResult(
    ShwapnoImportSummary Summary,
    Guid StoreId,
    Guid CategoryId);