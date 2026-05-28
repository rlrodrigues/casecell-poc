using CaseCellShop.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CaseCellShop.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ProductCatalogService>();
        services.AddScoped<CheckoutService>();
        services.AddScoped<OrderBillingProcessor>();
        return services;
    }
}
