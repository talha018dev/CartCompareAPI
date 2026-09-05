namespace CartCompareAPI.Canonicalization.Quantity;

public sealed record ParsedQuantity(
    decimal Value,
    string Unit,
    string MatchedText);
