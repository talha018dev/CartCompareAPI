namespace CartCompareAPI.Ingestion.Shwapno.Entities;

public sealed class UomOption
{
    public string Name { get; set; } = string.Empty;
    public decimal UnitValue { get; set; }
    public bool IsPreSelected { get; set; }
}
