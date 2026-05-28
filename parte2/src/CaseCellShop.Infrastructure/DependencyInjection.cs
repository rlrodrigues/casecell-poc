using CaseCellShop.Domain.Abstractions;
using CaseCellShop.Infrastructure.Cache;
using CaseCellShop.Infrastructure.Data;
using CaseCellShop.Infrastructure.External;
using CaseCellShop.Infrastructure.Messaging;
using CaseCellShop.Infrastructure.Repositories;
using CaseCellShop.Infrastructure.Time;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CaseCellShop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("ShopDb") ?? "Data Source=data/casecellshop.db";
            options.UseSqlite(connectionString);
        });

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            options.InstanceName = "casecellshop:";
        });

        services.AddScoped<IProductCatalogCache, RedisProductCatalogCache>();
        services.AddScoped<IProductCatalogRepository, EfProductCatalogRepository>();
        services.AddScoped<EfCheckoutStore>();
        services.AddScoped<ICheckoutStore>(provider => provider.GetRequiredService<EfCheckoutStore>());
        services.AddScoped<IOrderStatusReader>(provider => provider.GetRequiredService<EfCheckoutStore>());
        services.AddScoped<IOrderBillingStore>(provider => provider.GetRequiredService<EfCheckoutStore>());
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();
        services.AddScoped<IErpBillingClient, FakeErpBillingClient>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddMassTransit(configurator =>
        {
            configurator.AddConsumer<OrderCreatedConsumer>();

            configurator.AddEntityFrameworkOutbox<AppDbContext>(options =>
            {
                options.QueryDelay = TimeSpan.FromSeconds(1);
                options.UseSqlite();
                options.UseBusOutbox();
            });

            configurator.UsingInMemory((context, cfg) =>
            {
                cfg.UseMessageRetry(retry => retry.Exponential(
                    retryLimit: 3,
                    minInterval: TimeSpan.FromMilliseconds(200),
                    maxInterval: TimeSpan.FromSeconds(3),
                    intervalDelta: TimeSpan.FromMilliseconds(300)));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
