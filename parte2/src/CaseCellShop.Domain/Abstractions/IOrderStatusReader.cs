using CaseCellShop.Domain.Contracts;

namespace CaseCellShop.Domain.Abstractions;

public interface IOrderStatusReader
{
    Task<OrderStatusResponse?> GetStatusAsync(Guid orderId, CancellationToken cancellationToken);
}
