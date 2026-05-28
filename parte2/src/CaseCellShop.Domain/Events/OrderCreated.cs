namespace CaseCellShop.Domain.Events;

public sealed record OrderCreated(Guid OrderId, DateTimeOffset CreatedAt);
