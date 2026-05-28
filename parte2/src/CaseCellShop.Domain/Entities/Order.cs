namespace CaseCellShop.Domain.Entities;

public sealed class Order
{
    public Guid Id { get; set; }
    public required string IdempotencyKey { get; set; }
    public required string RequestHash { get; set; }
    public OrderStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<OrderLine> Lines { get; set; } = [];
    public List<StockReservation> Reservations { get; set; } = [];
}

public enum OrderStatus
{
    PendingBilling = 1,
    Billing = 2,
    Billed = 3,
    BillingFailed = 4
}
