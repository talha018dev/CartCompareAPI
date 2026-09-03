using System;
using System.Text;
using System.Text.RegularExpressions;

namespace CartCompareAPI.Canonicalization.Names;

public class ProductNameNormalizer : IProductNameNormalizer
{
    private static readonly Regex RepeatedWhitespace = new(
        @"\s+",
        RegexOptions.Compiled);
    public string Normalize(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return string.Empty;
        }

        string normalizedName = productName.Normalize(NormalizationForm.FormC);
        normalizedName = RepeatedWhitespace.Replace(normalizedName, " ");

        return normalizedName.Trim().ToLowerInvariant();
    }
}
