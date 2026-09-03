using System;
using System.Text;
using System.Text.RegularExpressions;

namespace CartCompareAPI.Canonicalization.Names;

public class ProductNameNormalizer : IProductNameNormalizer
{
    private static readonly Regex RepeatedWhitespace = new(
        @"\s+",
        RegexOptions.Compiled);

    private static readonly Regex Punctuation = new(
        @"[\p{P}\p{S}]+",
        RegexOptions.Compiled);

    private static readonly Regex InOneExpression = new(
        @"(?<=\d)\s*in\s*(?=\d)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    public string Normalize(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return string.Empty;
        }

        string normalizedName = productName.Normalize(NormalizationForm.FormC);
        normalizedName = Punctuation.Replace(normalizedName, " ");
        normalizedName = RepeatedWhitespace.Replace(normalizedName, " ");
        normalizedName = InOneExpression.Replace(normalizedName, " in ");
        return normalizedName.Trim().ToLowerInvariant();
    }


}
