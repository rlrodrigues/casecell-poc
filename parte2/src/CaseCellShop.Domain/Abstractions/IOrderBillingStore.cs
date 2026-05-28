using CaseCellShop.Domain.Entities;

namespace CaseCellShop.Domain.Abstractions;

public interface IOrderBillingStore
{
    Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    Task<Order?> GetOrderWithReservationsAsync(Guid orderId, CancellationToken cancellationToken);
    Task ConfirmReservationAsync(string sku, int quantity, DateTimeOffset confirmedAt, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
