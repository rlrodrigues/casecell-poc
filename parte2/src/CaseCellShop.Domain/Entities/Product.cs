namespace CaseCellShop.Domain.Entities;

public sealed class Product
{
    public int Id { get; set; }
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
