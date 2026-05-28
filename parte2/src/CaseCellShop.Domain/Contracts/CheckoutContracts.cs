namespace CaseCellShop.Domain.Contracts;

public sealed record CheckoutRequest(IReadOnlyList<CheckoutItemRequest> Items);

public sealed record CheckoutItemRequest(string Sku, int Quantity);

public sealed record CheckoutAcceptedResponse(Guid OrderId, string Status);

public sealed record OrderStatusResponse(
    Guid OrderId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<OrderLineResponse> Items);

public sealed record OrderLineResponse(string Sku, int Quantity, decimal UnitPrice);
