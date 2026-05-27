namespace CaseCellShop.Api.Services;

public sealed class FakeErpBillingClient(IConfiguration configuration, ILogger<FakeErpBillingClient> logger) : IErpBillingClient
{
    public async Task BillAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var delayMs = configuration.GetValue("Erp:BillingDelayMs", 250);
        logger.LogInformation("erp_billing_started orderId={OrderId} delayMs={DelayMs}", orderId, delayMs);
        await Task.Delay(delayMs, cancellationToken);
        logger.LogInformation("erp_billing_succeeded orderId={OrderId}", orderId);
    }
}
