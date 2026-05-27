using MassTransit;

namespace CaseCellShop.Api.Messaging;

public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishOrderCreatedAsync(Guid orderId, DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        return publishEndpoint.Publish(new OrderCreated(orderId, createdAt), cancellationToken);
    }
}
