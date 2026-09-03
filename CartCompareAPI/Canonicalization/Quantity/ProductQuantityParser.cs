using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CartCompareAPI.Canonicalization.Quantity;

public class ProductQuantityParser : IQuantityParser
{

    private static readonly Regex QuantityPattern = new(
        @"(?<![\p{L}\p{N}])(?<value>\d+(?:\.\d+)?)\s*(?<unit>kg|gm|g|ml|l|pcs|pc|pieces?|piece)(?!\p{L})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    public ParsedQuantity? Parse(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return null;
        }

        MatchCollection matches = QuantityPattern.Matches(productName);

        if (matches.Count == 0)
        {
            return null;
        }

        Match match = matches[0];
        string matchedValue = match.Groups["value"].Value;
        string matchedUnit = match.Groups["unit"].Value.ToLowerInvariant();

        if (matches.Count != 1)
        {
            return null;
        }

        if (!decimal.TryParse(
                matchedValue,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out decimal value
        ))
        {
            return null;
        }

        if (value <= 0) return null;

        return matchedUnit switch
        {
            "kg" => new ParsedQuantity(value * 1000, "g"),
            "gm" or "g" => new ParsedQuantity(value, "g"),
            "l" => new ParsedQuantity(value * 1000, "ml"),
            "ml" => new ParsedQuantity(value, "ml"),
            "pc" or "pcs" or "piece" or "pieces" => new ParsedQuantity(value, "count"),
            _ => null
        };
    }
}
