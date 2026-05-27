using CaseCellShop.Api.Data;
using CaseCellShop.Api.Services;
using CaseCellShop.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CaseCellShop.Tests;

public sealed class ProductCatalogServiceTests
{
    [Fact]
    public async Task GetProductsAsync_ShouldServeSecondReadFromCache()
    {
        await using var factory = new TestAppFactory(initialStock: 3);

        using var firstScope = factory.CreateScope();
        var catalog = firstScope.ServiceProvider.GetRequiredService<ProductCatalogService>();

        var firstRead = await catalog.GetProductsAsync(CancellationToken.None);

        using (var updateScope = factory.CreateScope())
        {
            var db = updateScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Products
                .Where(product => product.Sku == "CASE-TEST")
                .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.Name, "Nome alterado"));
        }

        var secondRead = await catalog.GetProductsAsync(CancellationToken.None);

        firstRead.Should().ContainSingle(product => product.Name == "Capinha de Teste");
        secondRead.Should().ContainSingle(product => product.Name == "Capinha de Teste");
    }
}
