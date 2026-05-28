using CaseCellShop.Domain.Abstractions;
using CaseCellShop.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CaseCellShop.Application.Services;

public sealed class OrderBillingProcessor(
    IOrderBillingStore store,
    IErpBillingClient erp,
    IProductCatalogCache catalogCache,
    IApplicationMetrics metrics,
    IClock clock,
    ILogger<OrderBillingProcessor> logger)
{
    public async Task ProcessAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await store.GetOrderWithReservationsAsync(orderId, cancellationToken);
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
        order.UpdatedAt = clock.UtcNow;
        await store.SaveChangesAsync(cancellationToken);

        try
        {
            await erp.BillAsync(orderId, cancellationToken);

            await using var transaction = await store.BeginTransactionAsync(cancellationToken);
            var billedAt = clock.UtcNow;
            order.Status = OrderStatus.Billed;
            order.UpdatedAt = billedAt;

            foreach (var reservation in order.Reservations.Where(candidate => candidate.Status == ReservationStatus.Reserved))
            {
                reservation.Status = ReservationStatus.Confirmed;
                await store.ConfirmReservationAsync(reservation.Sku, reservation.Quantity, billedAt, cancellationToken);
            }

            await store.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await catalogCache.InvalidateAsync(cancellationToken);

            metrics.BillingSucceeded();
            logger.LogInformation("order_billed orderId={OrderId}", orderId);
        }
        catch
        {
            metrics.BillingFailed();
            throw;
        }
    }
}
