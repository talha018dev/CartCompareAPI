namespace CartCompareAPI.Canonicalization.Names;

public interface IProductNameNormalizer
{
    string Normalize(string productName);
}