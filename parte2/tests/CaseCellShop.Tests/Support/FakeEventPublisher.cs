using System.Collections.Concurrent;
using CaseCellShop.Domain.Abstractions;

namespace CaseCellShop.Tests.Support;

public sealed class FakeEventPublisher : IEventPublisher
{
    private readonly ConcurrentBag<Guid> _publishedOrders = [];

    public IReadOnlyCollection<Guid> PublishedOrders => _publishedOrders.ToArray();

    public Task PublishOrderCreatedAsync(Guid orderId, DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        _publishedOrders.Add(orderId);
        return Task.CompletedTask;
    }
}
