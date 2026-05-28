using CaseCellShop.Domain.Contracts;

namespace CaseCellShop.Domain.Abstractions;

public interface IProductCatalogRepository
{
    Task<IReadOnlyList<ProductResponse>> ListProductsAsync(CancellationToken cancellationToken);
}
