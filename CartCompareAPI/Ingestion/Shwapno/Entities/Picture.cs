namespace CartCompareApi.Ingestion.Shwapno.Entities;

public class Picture
{
    public LargeDeviceUrl LargeDeviceUrl { get; set; } = null!;
    public SmallDeviceUrl SmallDeviceUrl { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string AlternateText { get; set; } = string.Empty;
}
