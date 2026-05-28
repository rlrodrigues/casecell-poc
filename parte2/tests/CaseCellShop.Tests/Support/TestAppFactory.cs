using CaseCellShop.Application.Services;
using CaseCellShop.Domain.Abstractions;
using CaseCellShop.Domain.Entities;
using CaseCellShop.Infrastructure.Cache;
using CaseCellShop.Infrastructure.Data;
using CaseCellShop.Infrastructure.Repositories;
using CaseCellShop.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CaseCellShop.Tests.Support;

public sealed class TestAppFactory : IAsyncDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"casecellshop-tests-{Guid.NewGuid():N}.db");

    public FakeEventPublisher Publisher { get; } = new();

    public ServiceProvider Services { get; }

    public TestAppFactory(int initialStock = 1)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddDistributedMemoryCache();
        serviceCollection.AddSingleton<IEventPublisher>(Publisher);
        serviceCollection.AddSingleton<IApplicationMetrics, NoopApplicationMetrics>();
        serviceCollection.AddSingleton<IClock, SystemClock>();
        serviceCollection.AddScoped<IProductCatalogCache, RedisProductCatalogCache>();
        serviceCollection.AddScoped<IProductCatalogRepository, EfProductCatalogRepository>();
        serviceCollection.AddScoped<EfCheckoutStore>();
        serviceCollection.AddScoped<ICheckoutStore>(provider => provider.GetRequiredService<EfCheckoutStore>());
        serviceCollection.AddScoped<IOrderStatusReader>(provider => provider.GetRequiredService<EfCheckoutStore>());
        serviceCollection.AddScoped<IOrderBillingStore>(provider => provider.GetRequiredService<EfCheckoutStore>());
        serviceCollection.AddScoped<ProductCatalogService>();
        serviceCollection.AddScoped<CheckoutService>();
        serviceCollection.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={_dbPath};Pooling=False"));

        Services = serviceCollection.BuildServiceProvider(validateScopes: true);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        Seed(db, initialStock);
    }

    public IServiceScope CreateScope() => Services.CreateScope();

    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();

        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private static void Seed(AppDbContext db, int initialStock)
    {
        var now = DateTimeOffset.UtcNow;
        db.Products.Add(new Product
        {
            Sku = "CASE-TEST",
            Name = "Capinha de Teste",
            Description = "Produto usado nos testes automatizados.",
            Price = 10.0m,
            UpdatedAt = now
        });
        db.Inventory.Add(new InventoryItem
        {
            Sku = "CASE-TEST",
            Available = initialStock,
            Reserved = 0,
            UpdatedAt = now
        });
        db.SaveChanges();
    }
}
