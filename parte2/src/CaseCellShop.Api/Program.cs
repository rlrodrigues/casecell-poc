using CaseCellShop.Api.Contracts;
using CaseCellShop.Api.Data;
using CaseCellShop.Api.Messaging;
using CaseCellShop.Api.Observability;
using CaseCellShop.Api.Services;
using MassTransit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ShopDb") ?? "Data Source=data/casecellshop.db";
    options.UseSqlite(connectionString);
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "casecellshop:";
});

builder.Services.AddScoped<ProductCatalogService>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<IEventPublisher, MassTransitEventPublisher>();
builder.Services.AddScoped<IErpBillingClient, FakeErpBillingClient>();

builder.Services.AddMassTransit(configurator =>
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("CaseCellShop.Api"))
    .WithTracing(tracing => tracing
        .AddSource(TraceSources.Name)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

app.MapGet("/products", async (ProductCatalogService catalog, CancellationToken cancellationToken) =>
    {
        using var activity = TraceSources.ActivitySource.StartActivity("products.list");
        var products = await catalog.GetProductsAsync(cancellationToken);
        return Results.Ok(products);
    })
    .WithName("GetProducts")
    .WithOpenApi();

app.MapPost("/checkout", async (
        CheckoutRequest request,
        HttpRequest httpRequest,
        CheckoutService checkout,
        CancellationToken cancellationToken) =>
    {
        using var activity = TraceSources.ActivitySource.StartActivity("checkout.start");
        var idempotencyKey = httpRequest.Headers["Idempotency-Key"].ToString();
        var result = await checkout.StartCheckoutAsync(request, idempotencyKey, cancellationToken);

        return result.Kind switch
        {
            CheckoutResultKind.Accepted => Results.Accepted(
                $"/orders/{result.OrderId}/status",
                new CheckoutAcceptedResponse(result.OrderId!.Value, result.Status!.Value.ToString())),
            CheckoutResultKind.Conflict => Results.Conflict(new { error = result.Message }),
            CheckoutResultKind.OutOfStock => Results.UnprocessableEntity(new { error = result.Message }),
            _ => Results.BadRequest(new { error = result.Message })
        };
    })
    .WithName("StartCheckout")
    .WithOpenApi();

app.MapGet("/orders/{orderId:guid}/status", async (
        Guid orderId,
        CheckoutService checkout,
        CancellationToken cancellationToken) =>
    {
        using var activity = TraceSources.ActivitySource.StartActivity("orders.status");
        var status = await checkout.GetStatusAsync(orderId, cancellationToken);
        return status is null ? Results.NotFound(new { error = "Pedido nao encontrado." }) : Results.Ok(status);
    })
    .WithName("GetOrderStatus")
    .WithOpenApi();

EnsureSqliteDirectory(app.Configuration);
await SeedData.EnsureCreatedAsync(app.Services);
await app.RunAsync();

static void EnsureSqliteDirectory(IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("ShopDb") ?? "Data Source=data/casecellshop.db";
    var builder = new SqliteConnectionStringBuilder(connectionString);
    var dataSource = builder.DataSource;

    if (string.IsNullOrWhiteSpace(dataSource) || dataSource is ":memory:")
    {
        return;
    }

    var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }
}

public partial class Program;
