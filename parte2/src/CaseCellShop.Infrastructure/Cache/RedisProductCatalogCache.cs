using System.Text.Json;
using CaseCellShop.Domain.Abstractions;
using CaseCellShop.Domain.Contracts;
using Microsoft.Extensions.Caching.Distributed;

namespace CaseCellShop.Infrastructure.Cache;

public sealed class RedisProductCatalogCache(IDistributedCache cache) : IProductCatalogCache
{
    private const string CacheKey = "catalog:products:v2";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<ProductResponse>?> GetAsync(CancellationToken cancellationToken)
    {
        var cached = await cache.GetStringAsync(CacheKey, cancellationToken);
        return cached is null
            ? null
            : JsonSerializer.Deserialize<IReadOnlyList<ProductResponse>>(cached, JsonOptions);
    }

    public Task SetAsync(IReadOnlyList<ProductResponse> products, CancellationToken cancellationToken)
    {
        var ttl = TimeSpan.FromSeconds(Random.Shared.Next(45, 76));
        return cache.SetStringAsync(
            CacheKey,
            JsonSerializer.Serialize(products, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken);
    }

    public Task InvalidateAsync(CancellationToken cancellationToken)
    {
        return cache.RemoveAsync(CacheKey, cancellationToken);
    }
}
