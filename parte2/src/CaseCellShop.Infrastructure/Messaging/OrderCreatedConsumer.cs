using CaseCellShop.Application.Services;
using CaseCellShop.Domain.Events;
using MassTransit;

namespace CaseCellShop.Infrastructure.Messaging;

public sealed class OrderCreatedConsumer(OrderBillingProcessor processor) : IConsumer<OrderCreated>
{
    public Task Consume(ConsumeContext<OrderCreated> context)
    {
        return processor.ProcessAsync(context.Message.OrderId, context.CancellationToken);
    }
}
