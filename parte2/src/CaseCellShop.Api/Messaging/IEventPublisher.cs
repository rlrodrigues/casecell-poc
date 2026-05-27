namespace CaseCellShop.Api.Messaging;

public interface IEventPublisher
{
    Task PublishOrderCreatedAsync(Guid orderId, DateTimeOffset createdAt, CancellationToken cancellationToken);
}
