using CaseCellShop.Api.Data;
using CaseCellShop.Api.Domain;
using CaseCellShop.Api.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CaseCellShop.Api.Messaging;

public sealed class OrderCreatedConsumer(
    AppDbContext db,
    IErpBillingClient erp,
    ProductCatalogService catalog,
    ILogger<OrderCreatedConsumer> logger) : IConsumer<OrderCreated>
{
    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var orderId = context.Message.OrderId;
        var order = await db.Orders
            .Include(candidate => candidate.Reservations)
            .FirstOrDefaultAsync(candidate => candidate.Id == orderId, context.CancellationToken);

        if (order is null)
        {
            logger.LogWarning("order_message_without_order orderId={OrderId}", orderId);
            return;
        }

        if (order.Status is OrderStatus.Billed)
        {
            logger.LogInformation("order_already_billed orderId={OrderId}", orderId);
            return;
        }

        order.Status = OrderStatus.Billing;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(context.CancellationToken);

        await erp.BillAsync(orderId, context.CancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(context.CancellationToken);

        order.Status = OrderStatus.Billed;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        foreach (var reservation in order.Reservations.Where(candidate => candidate.Status == ReservationStatus.Reserved))
        {
            reservation.Status = ReservationStatus.Confirmed;

            await db.Inventory
                .Where(inventory => inventory.Sku == reservation.Sku && inventory.Reserved >= reservation.Quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(inventory => inventory.Reserved, inventory => inventory.Reserved - reservation.Quantity)
                    .SetProperty(inventory => inventory.UpdatedAt, DateTimeOffset.UtcNow),
                    context.CancellationToken);
        }

        await db.SaveChangesAsync(context.CancellationToken);
        await transaction.CommitAsync(context.CancellationToken);
        await catalog.InvalidateAsync(context.CancellationToken);

        logger.LogInformation("order_billed orderId={OrderId}", orderId);
    }
}
