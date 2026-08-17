namespace CartCompareApi.Ingestion.Shwapno.Entities;

public class Picture
{
    public LargeDeviceUrl largeDeviceUrl { get; set; }
    public SmallDeviceUrl smallDeviceUrl { get; set; }
    public string title { get; set; }
    public string alternateText { get; set; }
}
