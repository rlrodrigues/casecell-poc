using System.Text.Json;
using CaseCellShop.Api.Contracts;
using CaseCellShop.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace CaseCellShop.Api.Services;

public sealed class ProductCatalogService(
    AppDbContext db,
    IDistributedCache cache,
    ILogger<ProductCatalogService> logger)
{
    private const string CacheKey = "catalog:products:v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<ProductResponse>> GetProductsAsync(CancellationToken cancellationToken)
    {
        var cached = await cache.GetStringAsync(CacheKey, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("catalog_cache_hit cacheKey={CacheKey}", CacheKey);
            return JsonSerializer.Deserialize<IReadOnlyList<ProductResponse>>(cached, JsonOptions) ?? [];
        }

        logger.LogInformation("catalog_cache_miss cacheKey={CacheKey}", CacheKey);

        var products = await db.Products
            .AsNoTracking()
            .Join(
                db.Inventory.AsNoTracking(),
                product => product.Sku,
                inventory => inventory.Sku,
                (product, inventory) => new { product, inventory })
            .OrderBy(row => row.product.Name)
            .Select(row => new ProductResponse(
                    row.product.Sku,
                    row.product.Name,
                    row.product.Description,
                    row.product.Price,
                    row.inventory.Available,
                    row.product.UpdatedAt))
            .ToListAsync(cancellationToken);

        var ttl = TimeSpan.FromSeconds(Random.Shared.Next(45, 76));
        await cache.SetStringAsync(
            CacheKey,
            JsonSerializer.Serialize(products, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken);

        return products;
    }

    public Task InvalidateAsync(CancellationToken cancellationToken)
    {
        return cache.RemoveAsync(CacheKey, cancellationToken);
    }
}
