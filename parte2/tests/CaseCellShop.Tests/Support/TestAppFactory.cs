using CaseCellShop.Api.Data;
using CaseCellShop.Api.Messaging;
using CaseCellShop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
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
        serviceCollection.AddLogging(builder => builder.AddDebug());
        serviceCollection.AddDistributedMemoryCache();
        serviceCollection.AddSingleton<IEventPublisher>(Publisher);
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
        db.Products.Add(new()
        {
            Sku = "CASE-TEST",
            Name = "Capinha de Teste",
            Description = "Produto usado nos testes automatizados.",
            Price = 10.0m,
            UpdatedAt = now
        });
        db.Inventory.Add(new()
        {
            Sku = "CASE-TEST",
            Available = initialStock,
            Reserved = 0,
            UpdatedAt = now
        });
        db.SaveChanges();
    }
}
