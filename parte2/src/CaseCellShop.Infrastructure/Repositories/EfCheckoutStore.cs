using CaseCellShop.Domain.Abstractions;
using CaseCellShop.Domain.Entities;
using CaseCellShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CaseCellShop.Infrastructure.Repositories;

public sealed class EfCheckoutStore(AppDbContext db) : ICheckoutStore, IOrderStatusReader, IOrderBillingStore
{
    public async Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        return new EfAppTransaction(await db.Database.BeginTransactionAsync(cancellationToken));
    }

    public Task<Order?> FindOrderByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        return db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(order => order.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetPricesAsync(IReadOnlyCollection<string> skus, CancellationToken cancellationToken)
    {
        return await db.Products
            .AsNoTracking()
            .Where(product => skus.Contains(product.Sku))
            .ToDictionaryAsync(product => product.Sku, product => product.Price, StringComparer.OrdinalIgnoreCase, cancellationToken);
    }

    public async Task<bool> TryReserveStockAsync(string sku, int quantity, DateTimeOffset reservedAt, CancellationToken cancellationToken)
    {
        var rows = await db.Inventory
            .Where(inventory => inventory.Sku == sku && inventory.Available >= quantity)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(inventory => inventory.Available, inventory => inventory.Available - quantity)
                .SetProperty(inventory => inventory.Reserved, inventory => inventory.Reserved + quantity)
                .SetProperty(inventory => inventory.UpdatedAt, reservedAt),
                cancellationToken);

        return rows == 1;
    }

    public void AddOrder(Order order)
    {
        db.Orders.Add(order);
    }

    public async Task<Domain.Contracts.OrderStatusResponse?> GetStatusAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await db.Orders
            .AsNoTracking()
            .Where(order => order.Id == orderId)
            .Select(order => new Domain.Contracts.OrderStatusResponse(
                order.Id,
                order.Status.ToString(),
                order.CreatedAt,
                order.UpdatedAt,
                order.Lines
                    .OrderBy(line => line.Sku)
                    .Select(line => new Domain.Contracts.OrderLineResponse(line.Sku, line.Quantity, line.UnitPrice))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Order?> GetOrderWithReservationsAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return db.Orders
            .Include(order => order.Reservations)
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    public Task ConfirmReservationAsync(string sku, int quantity, DateTimeOffset confirmedAt, CancellationToken cancellationToken)
    {
        return db.Inventory
            .Where(inventory => inventory.Sku == sku && inventory.Reserved >= quantity)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(inventory => inventory.Reserved, inventory => inventory.Reserved - quantity)
                .SetProperty(inventory => inventory.UpdatedAt, confirmedAt),
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return db.SaveChangesAsync(cancellationToken);
    }
}
