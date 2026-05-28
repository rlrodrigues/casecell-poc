using CaseCellShop.Domain.Abstractions;
using CaseCellShop.Domain.Contracts;
using Microsoft.Extensions.Logging;

namespace CaseCellShop.Application.Services;

public sealed class ProductCatalogService(
    IProductCatalogRepository repository,
    IProductCatalogCache cache,
    IApplicationMetrics metrics,
    ILogger<ProductCatalogService> logger)
{
    public async Task<IReadOnlyList<ProductResponse>> GetProductsAsync(CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync(cancellationToken);
        if (cached is not null)
        {
            metrics.CacheHit();
            logger.LogInformation("catalog_cache_hit");
            return cached;
        }

        metrics.CacheMiss();
        logger.LogInformation("catalog_cache_miss");

        var products = await repository.ListProductsAsync(cancellationToken);
        await cache.SetAsync(products, cancellationToken);
        return products;
    }

    public Task InvalidateAsync(CancellationToken cancellationToken)
    {
        return cache.InvalidateAsync(cancellationToken);
    }
}
