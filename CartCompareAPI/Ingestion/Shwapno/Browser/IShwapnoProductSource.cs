using CartCompareAPI.Ingestion.Shwapno.Entities;

namespace CartCompareAPI.Ingestion.Shwapno.Browser;

public interface IShwapnoProductSource
{
    Task<IReadOnlyCollection<ShwapnoProduct>> GetProductsFromShwapno(
        string category,
        CancellationToken cancellationToken = default);
}