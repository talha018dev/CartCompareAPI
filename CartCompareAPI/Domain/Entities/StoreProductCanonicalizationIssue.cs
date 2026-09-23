using CartCompareAPI.Canonicalization.StoreProducts;

namespace CartCompareAPI.Domain.Entities;

public sealed class StoreProductCanonicalizationIssue
{
    public Guid Id { get; set; }
    public Guid StoreProductId { get; set; }
    public string StoreProductName { get; set; } = null!;
    public Guid StoreId { get; set; }
    public string StoreName { get; set; } = null!;
    public StoreProductCanonicalizationOutcome Outcome { get; set; }
    public StoreProductCanonicalizationFailure FailureReason { get; set; }
    public int AttemptCount { get; set; }
    public DateTime FirstOccurredAt { get; set; }
    public DateTime LastOccurredAt { get; set; }

    public StoreProduct StoreProduct { get; set; } = null!;
    public Store Store { get; set; } = null!;
}
