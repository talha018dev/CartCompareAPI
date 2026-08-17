namespace CartCompareApi.Ingestion.Shwapno.Entities;

public class Price
{
    public string price { get; set; }
    public int priceValue { get; set; }
    public int discountAmountValue { get; set; }
    public int unitPriceValue { get; set; }
    public int discountMaxQuantity { get; set; }
    public int discountMinQuantity { get; set; }
    public string oldPrice { get; set; }
    public int? oldPriceValue { get; set; }
    public string discountAmount { get; set; }
}
