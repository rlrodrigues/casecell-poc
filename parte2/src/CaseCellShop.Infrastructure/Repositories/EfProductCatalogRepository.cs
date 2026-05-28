using CaseCellShop.Domain.Abstractions;
using CaseCellShop.Domain.Contracts;
using CaseCellShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CaseCellShop.Infrastructure.Repositories;

public sealed class EfProductCatalogRepository(AppDbContext db) : IProductCatalogRepository
{
    public async Task<IReadOnlyList<ProductResponse>> ListProductsAsync(CancellationToken cancellationToken)
    {
        return await db.Products
            .AsNoTracking()
            .Join(
                db.Inventory.AsNoTracking(),
                product => product.Sku,
                inventory => inventory.Sku,
                (product, inventory) => new { product, inventory })
            .OrderBy(row => row.product.Name)
            .Select(row => new ProductResponse(
                row.product.Sku,
                row.product.Name,
                row.product.Description,
                row.product.Price,
                row.inventory.Available,
                row.product.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
