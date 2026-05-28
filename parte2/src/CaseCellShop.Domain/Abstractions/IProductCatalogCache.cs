using CaseCellShop.Domain.Contracts;

namespace CaseCellShop.Domain.Abstractions;

public interface IProductCatalogCache
{
    Task<IReadOnlyList<ProductResponse>?> GetAsync(CancellationToken cancellationToken);
    Task SetAsync(IReadOnlyList<ProductResponse> products, CancellationToken cancellationToken);
    Task InvalidateAsync(CancellationToken cancellationToken);
}
