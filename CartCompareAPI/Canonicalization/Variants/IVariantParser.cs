namespace CartCompareAPI.Canonicalization.Variants;

public interface IVariantParser
{
    ParsedVariant? Parse(string normalizedProductName);
}
