namespace CartCompareAPI.Canonicalization.Quantity;

public interface IQuantityParser
{
    ParsedQuantity? Parse(
        string productName,
        string? fallbackQuantityText = null);
}
