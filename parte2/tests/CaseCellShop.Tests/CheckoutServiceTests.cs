using CaseCellShop.Application.Services;
using CaseCellShop.Domain.Contracts;
using CaseCellShop.Domain.Entities;
using CaseCellShop.Infrastructure.Data;
using CaseCellShop.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CaseCellShop.Tests;

public sealed class CheckoutServiceTests
{
    [Fact]
    public async Task StartCheckoutAsync_ShouldBeIdempotentForSameKeyAndPayload()
    {
        await using var factory = new TestAppFactory(initialStock: 5);
        using var scope = factory.CreateScope();
        var checkout = scope.ServiceProvider.GetRequiredService<CheckoutService>();
        var request = new CheckoutRequest([new CheckoutItemRequest("CASE-TEST", 1)]);

        var first = await checkout.StartCheckoutAsync(request, "same-key", CancellationToken.None);
        var second = await checkout.StartCheckoutAsync(request, "same-key", CancellationToken.None);

        first.Kind.Should().Be(CheckoutResultKind.Accepted);
        second.Kind.Should().Be(CheckoutResultKind.Accepted);
        second.OrderId.Should().Be(first.OrderId);
        factory.Publisher.PublishedOrders.Should().ContainSingle();
    }

    [Fact]
    public async Task StartCheckoutAsync_ShouldRejectSameIdempotencyKeyWithDifferentPayload()
    {
        await using var factory = new TestAppFactory(initialStock: 5);
        using var scope = factory.CreateScope();
        var checkout = scope.ServiceProvider.GetRequiredService<CheckoutService>();

        var first = await checkout.StartCheckoutAsync(
            new CheckoutRequest([new CheckoutItemRequest("CASE-TEST", 1)]),
            "same-key",
            CancellationToken.None);

        var second = await checkout.StartCheckoutAsync(
            new CheckoutRequest([new CheckoutItemRequest("CASE-TEST", 2)]),
            "same-key",
            CancellationToken.None);

        first.Kind.Should().Be(CheckoutResultKind.Accepted);
        second.Kind.Should().Be(CheckoutResultKind.Conflict);
    }

    [Fact]
    public async Task StartCheckoutAsync_ShouldPreventOversellingUnderConcurrentRequests()
    {
        await using var factory = new TestAppFactory(initialStock: 1);
        var request = new CheckoutRequest([new CheckoutItemRequest("CASE-TEST", 1)]);

        var tasks = Enumerable.Range(0, 12)
            .Select(index => Task.Run(async () =>
            {
                using var scope = factory.CreateScope();
                var checkout = scope.ServiceProvider.GetRequiredService<CheckoutService>();
                return await checkout.StartCheckoutAsync(request, $"key-{index}", CancellationToken.None);
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        results.Count(result => result.Kind == CheckoutResultKind.Accepted).Should().Be(1);
        results.Count(result => result.Kind == CheckoutResultKind.OutOfStock).Should().Be(11);

        using var verificationScope = factory.CreateScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var inventory = await db.Inventory.AsNoTracking().SingleAsync(item => item.Sku == "CASE-TEST");
        var orderCount = await db.Orders.CountAsync();
        var reservationCount = await db.StockReservations.CountAsync(reservation => reservation.Status == ReservationStatus.Reserved);

        inventory.Available.Should().Be(0);
        inventory.Reserved.Should().Be(1);
        orderCount.Should().Be(1);
        reservationCount.Should().Be(1);
    }
}
