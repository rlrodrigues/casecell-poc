using CaseCellShop.Application;
using CaseCellShop.Application.Services;
using CaseCellShop.Domain.Abstractions;
using CaseCellShop.Domain.Contracts;
using CaseCellShop.Infrastructure;
using CaseCellShop.Infrastructure.Data;
using CaseCellShop.Api.Observability;
using Microsoft.Data.Sqlite;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IApplicationMetrics, ApplicationMetrics>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var serviceName = builder.Configuration["Observability:ServiceName"] ?? "CaseCellShop.Api";
var otlpEndpoint = builder.Configuration["Observability:OtlpEndpoint"];

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.ParseStateValues = true;
    logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));

    if (!string.IsNullOrWhiteSpace(otlpEndpoint))
    {
        logging.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
    }

    logging.AddConsoleExporter();
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddSource(TraceSources.Name)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporterIfConfigured(otlpEndpoint))
    .WithMetrics(metrics => metrics
        .AddMeter(ApplicationMetrics.MeterName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporterIfConfigured(otlpEndpoint));

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

internal static class OpenTelemetryBuilderExtensions
{
    public static TracerProviderBuilder AddOtlpExporterIfConfigured(this TracerProviderBuilder builder, string? endpoint)
    {
        return string.IsNullOrWhiteSpace(endpoint)
            ? builder
            : builder.AddOtlpExporter(options => options.Endpoint = new Uri(endpoint));
    }

    public static MeterProviderBuilder AddOtlpExporterIfConfigured(this MeterProviderBuilder builder, string? endpoint)
    {
        return string.IsNullOrWhiteSpace(endpoint)
            ? builder
            : builder.AddOtlpExporter(options => options.Endpoint = new Uri(endpoint));
    }
}
