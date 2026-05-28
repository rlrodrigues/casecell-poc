using CaseCellShop.Domain.Entities;

namespace CaseCellShop.Domain.Abstractions;

public interface ICheckoutStore
{
    Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    Task<Order?> FindOrderByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, decimal>> GetPricesAsync(IReadOnlyCollection<string> skus, CancellationToken cancellationToken);
    Task<bool> TryReserveStockAsync(string sku, int quantity, DateTimeOffset reservedAt, CancellationToken cancellationToken);
    void AddOrder(Order order);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
