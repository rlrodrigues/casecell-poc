namespace CaseCellShop.Api.Contracts;

public sealed record ProductResponse(
    string Sku,
    string Name,
    string Description,
    decimal Price,
    int Available,
    DateTimeOffset UpdatedAt);
