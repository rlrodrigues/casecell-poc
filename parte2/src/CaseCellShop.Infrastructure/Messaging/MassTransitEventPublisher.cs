using CaseCellShop.Domain.Abstractions;
using CaseCellShop.Domain.Events;
using MassTransit;

namespace CaseCellShop.Infrastructure.Messaging;

public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishOrderCreatedAsync(Guid orderId, DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        return publishEndpoint.Publish(new OrderCreated(orderId, createdAt), cancellationToken);
    }
}
