namespace CaseCellShop.Api.Messaging;

public sealed record OrderCreated(Guid OrderId, DateTimeOffset CreatedAt);
