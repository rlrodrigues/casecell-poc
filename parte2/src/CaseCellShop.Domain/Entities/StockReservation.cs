namespace CaseCellShop.Domain.Entities;

public sealed class StockReservation
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public required string Sku { get; set; }
    public int Quantity { get; set; }
    public ReservationStatus Status { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public Order? Order { get; set; }
}

public enum ReservationStatus
{
    Reserved = 1,
    Confirmed = 2,
    Released = 3,
    Expired = 4
}
