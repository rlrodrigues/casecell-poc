using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CaseCellShop.Api.Contracts;
using CaseCellShop.Api.Data;
using CaseCellShop.Api.Domain;
using CaseCellShop.Api.Messaging;
using Microsoft.EntityFrameworkCore;

namespace CaseCellShop.Api.Services;

public sealed class CheckoutService(
    AppDbContext db,
    IEventPublisher eventPublisher,
    ProductCatalogService catalog,
    ILogger<CheckoutService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CheckoutResult> StartCheckoutAsync(
        CheckoutRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request, idempotencyKey);
        if (validationError is not null)
        {
            return CheckoutResult.Invalid(validationError);
        }

        var requestHash = Hash(request);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var existing = await db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(order => order.IdempotencyKey == idempotencyKey, cancellationToken);

        if (existing is not null)
        {
            if (!StringComparer.Ordinal.Equals(existing.RequestHash, requestHash))
            {
                return CheckoutResult.Conflict("A chave de idempotencia ja foi usada com outro payload.");
            }

            return CheckoutResult.Accepted(existing.Id, existing.Status);
        }

        var skus = request.Items.Select(item => item.Sku).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var prices = await db.Products
            .AsNoTracking()
            .Where(product => skus.Contains(product.Sku))
            .ToDictionaryAsync(product => product.Sku, product => product.Price, StringComparer.OrdinalIgnoreCase, cancellationToken);

        if (prices.Count != skus.Length)
        {
            return CheckoutResult.Invalid("Um ou mais SKUs nao existem.");
        }

        var now = DateTimeOffset.UtcNow;
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            IdempotencyKey = idempotencyKey,
            RequestHash = requestHash,
            Status = OrderStatus.PendingBilling,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var item in request.Items)
        {
            var reserved = await db.Inventory
                .Where(inventory => inventory.Sku == item.Sku && inventory.Available >= item.Quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(inventory => inventory.Available, inventory => inventory.Available - item.Quantity)
                    .SetProperty(inventory => inventory.Reserved, inventory => inventory.Reserved + item.Quantity)
                    .SetProperty(inventory => inventory.UpdatedAt, now),
                    cancellationToken);

            if (reserved == 0)
            {
                logger.LogWarning("stock_reservation_rejected sku={Sku} quantity={Quantity}", item.Sku, item.Quantity);
                return CheckoutResult.OutOfStock(item.Sku);
            }

            order.Lines.Add(new OrderLine
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Sku = item.Sku,
                Quantity = item.Quantity,
                UnitPrice = prices[item.Sku]
            });

            order.Reservations.Add(new StockReservation
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Sku = item.Sku,
                Quantity = item.Quantity,
                Status = ReservationStatus.Reserved,
                ExpiresAt = now.AddMinutes(15)
            });
        }

        db.Orders.Add(order);
        await eventPublisher.PublishOrderCreatedAsync(orderId, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await catalog.InvalidateAsync(cancellationToken);

        logger.LogInformation("order_created orderId={OrderId} items={ItemCount}", orderId, request.Items.Count);

        return CheckoutResult.Accepted(orderId, OrderStatus.PendingBilling);
    }

    public async Task<OrderStatusResponse?> GetStatusAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await db.Orders
            .AsNoTracking()
            .Where(order => order.Id == orderId)
            .Select(order => new OrderStatusResponse(
                order.Id,
                order.Status.ToString(),
                order.CreatedAt,
                order.UpdatedAt,
                order.Lines
                    .OrderBy(line => line.Sku)
                    .Select(line => new OrderLineResponse(line.Sku, line.Quantity, line.UnitPrice))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string? ValidateRequest(CheckoutRequest request, string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return "O header Idempotency-Key e obrigatorio.";
        }

        if (request.Items.Count == 0)
        {
            return "O checkout precisa conter ao menos um item.";
        }

        if (request.Items.Any(item => string.IsNullOrWhiteSpace(item.Sku) || item.Quantity <= 0))
        {
            return "Todos os itens precisam ter SKU e quantidade positiva.";
        }

        return null;
    }

    private static string Hash(CheckoutRequest request)
    {
        var payload = JsonSerializer.Serialize(request, JsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }
}

public sealed record CheckoutResult(CheckoutResultKind Kind, Guid? OrderId, OrderStatus? Status, string? Message)
{
    public static CheckoutResult Accepted(Guid orderId, OrderStatus status) =>
        new(CheckoutResultKind.Accepted, orderId, status, null);

    public static CheckoutResult Invalid(string message) =>
        new(CheckoutResultKind.Invalid, null, null, message);

    public static CheckoutResult Conflict(string message) =>
        new(CheckoutResultKind.Conflict, null, null, message);

    public static CheckoutResult OutOfStock(string sku) =>
        new(CheckoutResultKind.OutOfStock, null, null, $"Estoque insuficiente para o SKU {sku}.");
}

public enum CheckoutResultKind
{
    Accepted,
    Invalid,
    Conflict,
    OutOfStock
}
