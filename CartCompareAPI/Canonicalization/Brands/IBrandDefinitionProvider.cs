namespace CartCompareAPI.Canonicalization.Brands;

public interface IBrandDefinitionProvider
{
    Task<IReadOnlyCollection<BrandDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
