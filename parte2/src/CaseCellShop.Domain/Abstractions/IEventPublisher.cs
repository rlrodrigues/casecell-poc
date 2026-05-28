namespace CaseCellShop.Domain.Abstractions;

public interface IEventPublisher
{
    Task PublishOrderCreatedAsync(Guid orderId, DateTimeOffset createdAt, CancellationToken cancellationToken);
}
