namespace CartCompareApi.Ingestion.Shwapno.Entities;

public class ShwapnoProduct
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int OrderMinimumQuantity { get; set; }
    public int OrderMaximumQuantity { get; set; }
    public int OrderPackageQuantity { get; set; }
    public bool AllowExpressDelivery { get; set; }
    public string SeName { get; set; } = string.Empty;
    public bool ShowReviewInProductBox { get; set; }
    public int RatingAverage { get; set; }
    public int TotalReviews { get; set; }
    public PriceClass Price { get; set; } = null!;
    public Picture Picture { get; set; } = null!;
    public bool EnableBuyNow { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string DeliveryInfo { get; set; } = string.Empty;
    public bool AllowAddToCart { get; set; }
    public bool CantAddOtherItems { get; set; }
    public int UomType { get; set; }
    public int DisplayOrder { get; set; }
    public string Stock { get; set; } = string.Empty;
    public bool IsQuickView { get; set; }
    public bool IsBestSeller { get; set; }
    public bool IsNew { get; set; }
    public List<Ribbon> Ribbons { get; set; } = null!;
    public string Status { get; set; } = string.Empty;
    public bool HasMultipleAttributes { get; set; }
    public string Id { get; set; } = string.Empty;
}
