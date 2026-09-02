
using CartCompareApi.Canonicalization.Quantity;

public interface IQuantityParser
{
    ParsedQuantity? Parse(string productName);
}