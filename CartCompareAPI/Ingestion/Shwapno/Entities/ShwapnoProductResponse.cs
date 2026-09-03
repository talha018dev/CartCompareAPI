namespace CartCompareAPI.Ingestion.Shwapno.Entities;

public class ShwapnoProductResponse
{
    public List<ShwapnoProduct> Products { get; set; } = null!;
    public int PageIndex { get; set; }
    public int PageNumber { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public int FirstItem { get; set; }
    public int LastItem { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}
