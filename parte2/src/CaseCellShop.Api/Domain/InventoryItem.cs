namespace CaseCellShop.Api.Domain;

public sealed class InventoryItem
{
    public required string Sku { get; set; }
    public int Available { get; set; }
    public int Reserved { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
