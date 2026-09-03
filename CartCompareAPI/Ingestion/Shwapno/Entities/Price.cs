namespace CartCompareAPI.Ingestion.Shwapno.Entities;

public class PriceClass
{
    public string Price { get; set; } = string.Empty;
    public decimal PriceValue { get; set; }
    public decimal discountAmountValue { get; set; }
    public decimal unitPriceValue { get; set; }
    public int discountMaxQuantity { get; set; }
    public int discountMinQuantity { get; set; }
    public string? oldPrice { get; set; }
    public decimal? oldPriceValue { get; set; }
    public string? discountAmount { get; set; }
}
